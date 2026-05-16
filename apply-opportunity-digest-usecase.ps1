param(
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-PathOrCreate {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (Test-Path $Path) {
        return (Resolve-Path $Path).Path
    }

    return (Join-Path (Get-Location) $Path)
}

function Write-Utf8NoBom {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $directory = Split-Path -Parent $Path
    if ($directory -and -not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText((Resolve-PathOrCreate $Path), $Content, $encoding)
}

function Invoke-Dotnet {
    param([Parameter(Mandatory = $true)][string[]]$CommandArgs)

    Write-Host "dotnet $($CommandArgs -join ' ')"
    & dotnet @CommandArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed: dotnet $($CommandArgs -join ' ')"
    }
}

if (-not (Test-Path ".\NewsSummarizer.sln")) {
    throw "Run this script from repository root. NewsSummarizer.sln was not found."
}

Write-Utf8NoBom "src/NewsSummarizer.Core/Models/UseCaseResults.cs" @'
namespace NewsSummarizer.Core.Models;

public sealed record FetchNewsSummary(
    int SourcesChecked,
    int ArticlesFetched,
    int ArticlesAdded,
    int DuplicateArticles,
    int SkippedArticles,
    int FailedSources);

public sealed record AnalyzeArticlesSummary(
    int ArticlesTaken,
    int ArticlesAnalyzed,
    int ArticlesFailed);

public sealed record BuildDailyDigestSummary(
    int UsersChecked,
    int DigestsCreated,
    int UsersSkippedDisabled,
    int UsersSkippedExistingDigest,
    int UsersSkippedNoItems);

public sealed record BuildOpportunityDigestSummary(
    int UsersChecked,
    int DigestsCreated,
    int UsersSkippedDisabled,
    int UsersSkippedExistingDigest,
    int UsersSkippedNoItems);
'@

Write-Utf8NoBom "src/NewsSummarizer.Core/UseCases/BuildOpportunityDigestUseCase.cs" @'
using System.Text;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.Services;

namespace NewsSummarizer.Core.UseCases;

public sealed class BuildOpportunityDigestUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IArticleAiResultRepository _articleAiResultRepository;
    private readonly IDigestRepository _digestRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly RetentionPolicyService _retentionPolicyService;

    public BuildOpportunityDigestUseCase(
        IUserRepository userRepository,
        IUserPreferencesRepository preferencesRepository,
        IArticleRepository articleRepository,
        IArticleAiResultRepository articleAiResultRepository,
        IDigestRepository digestRepository,
        INotificationRepository notificationRepository,
        RetentionPolicyService retentionPolicyService)
    {
        _userRepository = userRepository;
        _preferencesRepository = preferencesRepository;
        _articleRepository = articleRepository;
        _articleAiResultRepository = articleAiResultRepository;
        _digestRepository = digestRepository;
        _notificationRepository = notificationRepository;
        _retentionPolicyService = retentionPolicyService;
    }

    public async Task<BuildOpportunityDigestSummary> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var periodEnd = now;
        var periodStart = now.AddDays(-1);

        return await ExecuteAsync(periodStart, periodEnd, cancellationToken);
    }

    public async Task<BuildOpportunityDigestSummary> ExecuteAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetActiveAsync(cancellationToken);

        var usersChecked = 0;
        var digestsCreated = 0;
        var usersSkippedDisabled = 0;
        var usersSkippedExistingDigest = 0;
        var usersSkippedNoItems = 0;

        var articles = await _articleRepository.GetAnalyzedForPeriodAsync(
            periodStart,
            periodEnd,
            limit: 200,
            cancellationToken);

        var articleIds = articles.Select(article => article.Id).ToArray();

        var aiResults = await _articleAiResultRepository.GetSuccessfulByArticleIdsAsync(
            articleIds,
            cancellationToken);

        var latestResultByArticleId = aiResults
            .GroupBy(result => result.ArticleId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(result => result.CreatedAt).First());

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            usersChecked++;

            var preferences = await _preferencesRepository.GetByUserIdAsync(user.Id, cancellationToken);

            if (preferences is null || !preferences.OpportunityDigestEnabled)
            {
                usersSkippedDisabled++;
                continue;
            }

            var exists = await _digestRepository.ExistsAsync(
                user.Id,
                DigestType.Opportunity,
                periodStart,
                periodEnd,
                cancellationToken);

            if (exists)
            {
                usersSkippedExistingDigest++;
                continue;
            }

            var selectedItems = SelectItems(
                    articles,
                    latestResultByArticleId,
                    preferences)
                .OrderByDescending(item => item.AiResult.OpportunityScore)
                .ThenByDescending(item => item.AiResult.ImportanceScore)
                .Take(Math.Max(1, preferences.MaxItemsPerDigest))
                .ToList();

            if (selectedItems.Count == 0)
            {
                usersSkippedNoItems++;
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            var digest = new Digest
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DigestType = DigestType.Opportunity,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Status = DigestStatus.Created,
                CreatedAt = now,
                UpdatedAt = now
            };

            var digestItems = selectedItems
                .Select((item, index) => new DigestItem
                {
                    Id = Guid.NewGuid(),
                    DigestId = digest.Id,
                    ArticleId = item.Article.Id,
                    Position = index + 1,
                    TitleSnapshot = item.Article.Title,
                    UrlSnapshot = item.Article.Url,
                    SourceNameSnapshot = null,
                    SummarySnapshot = item.AiResult.Summary,
                    ReasonSnapshot = item.AiResult.OpportunityReason ?? item.AiResult.Reason,
                    CreatedAt = DateTimeOffset.UtcNow
                })
                .ToList();

            await _digestRepository.AddAsync(digest, digestItems, cancellationToken);

            var notificationDedupKey = BuildNotificationDedupKey(
                user.Id,
                DigestType.Opportunity,
                periodStart,
                periodEnd);

            if (!await _notificationRepository.ExistsAsync(
                    user.Id,
                    NotificationType.OpportunityDigest,
                    notificationDedupKey,
                    cancellationToken))
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    DigestId = digest.Id,
                    NotificationType = NotificationType.OpportunityDigest,
                    DedupKey = notificationDedupKey,
                    Status = NotificationStatus.Pending,
                    TitleSnapshot = "Opportunity digest",
                    MessageSnapshot = BuildDigestMessage(selectedItems),
                    ExpiresAt = _retentionPolicyService.GetNotificationExpiresAt(DateTimeOffset.UtcNow),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _notificationRepository.AddAsync(notification, cancellationToken);
            }

            await _digestRepository.SaveChangesAsync(cancellationToken);
            digestsCreated++;
        }

        return new BuildOpportunityDigestSummary(
            usersChecked,
            digestsCreated,
            usersSkippedDisabled,
            usersSkippedExistingDigest,
            usersSkippedNoItems);
    }

    private static IEnumerable<SelectedOpportunityDigestItem> SelectItems(
        IReadOnlyList<NewsArticle> articles,
        IReadOnlyDictionary<Guid, ArticleAiResult> aiResultByArticleId,
        UserPreferences preferences)
    {
        foreach (var article in articles)
        {
            if (!aiResultByArticleId.TryGetValue(article.Id, out var aiResult))
            {
                continue;
            }

            if (!aiResult.OpportunityDigestCandidate)
            {
                continue;
            }

            if (!MatchesUserCategories(aiResult, preferences))
            {
                continue;
            }

            yield return new SelectedOpportunityDigestItem(article, aiResult);
        }
    }

    private static bool MatchesUserCategories(ArticleAiResult aiResult, UserPreferences preferences)
    {
        if (preferences.EnabledCategories.Count == 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(aiResult.Category))
        {
            return true;
        }

        return preferences.EnabledCategories.Any(category =>
            string.Equals(category, aiResult.Category, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildNotificationDedupKey(
        Guid userId,
        DigestType digestType,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd)
    {
        return $"digest:{digestType}:{userId:N}:{periodStart:yyyyMMddHHmmss}:{periodEnd:yyyyMMddHHmmss}";
    }

    private static string BuildDigestMessage(IReadOnlyList<SelectedOpportunityDigestItem> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Opportunity digest");

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            builder.AppendLine();
            builder.AppendLine($"{i + 1}. {item.Article.Title}");

            if (!string.IsNullOrWhiteSpace(item.AiResult.Summary))
            {
                builder.AppendLine($"Summary: {item.AiResult.Summary}");
            }

            if (!string.IsNullOrWhiteSpace(item.AiResult.OpportunityReason))
            {
                builder.AppendLine($"Opportunity: {item.AiResult.OpportunityReason}");
            }
            else if (!string.IsNullOrWhiteSpace(item.AiResult.Reason))
            {
                builder.AppendLine($"Reason: {item.AiResult.Reason}");
            }

            builder.AppendLine($"Url: {item.Article.Url}");
        }

        return builder.ToString();
    }

    private sealed record SelectedOpportunityDigestItem(
        NewsArticle Article,
        ArticleAiResult AiResult);
}
'@

Write-Utf8NoBom "src/NewsSummarizer.Infrastructure/Fetching/MockNewsFetcher.cs" @'
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Infrastructure.Fetching;

public sealed class MockNewsFetcher : INewsFetcher
{
    public Task<IReadOnlyList<FetchedArticle>> FetchAsync(
        NewsSource source,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        IReadOnlyList<FetchedArticle> result =
        [
            new FetchedArticle(
                "Sample general news title",
                $"https://example.com/news/general-{now:yyyyMMddHHmm}",
                "Sample general description",
                "Sample general content",
                "en",
                now),

            new FetchedArticle(
                "AI startup market grows after new regulation",
                $"https://example.com/news/ai-startup-market-{now:yyyyMMddHHmm}",
                "A mock technology market article for opportunity digest testing.",
                "A mock technology market article that should be treated as an opportunity signal.",
                "en",
                now),

            new FetchedArticle(
                "Urgent market crisis alert from mock source",
                $"https://example.com/news/urgent-market-crisis-{now:yyyyMMddHHmm}",
                "A mock urgent article for future urgent notification testing.",
                "A mock urgent article for future urgent notification testing.",
                "en",
                now)
        ];

        return Task.FromResult(result);
    }
}
'@

Write-Utf8NoBom "src/NewsSummarizer.Api/Program.cs" @'
using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Ai;
using NewsSummarizer.Core;
using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Infrastructure;
using NewsSummarizer.Infrastructure.Persistence;
using NewsSummarizer.Telegram;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAi(builder.Configuration);
builder.Services.AddTelegram(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "news-summarizer"
}));

app.MapPost("/debug/seed", async (
    DatabaseSeeder seeder,
    CancellationToken cancellationToken) =>
{
    await seeder.SeedAsync(cancellationToken);

    return Results.Ok(new
    {
        status = "seeded"
    });
});

app.MapPost("/debug/fetch-news", async (
    FetchNewsUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(cancellationToken);

    return Results.Ok(result);
});

app.MapPost("/debug/analyze-articles", async (
    AnalyzeArticleUseCase useCase,
    CancellationToken cancellationToken,
    int limit = 20) =>
{
    var result = await useCase.ExecuteAsync(limit, cancellationToken);

    return Results.Ok(result);
});

app.MapPost("/debug/build-daily-digests", async (
    BuildDailyDigestUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(cancellationToken);

    return Results.Ok(result);
});

app.MapPost("/debug/build-opportunity-digests", async (
    BuildOpportunityDigestUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(cancellationToken);

    return Results.Ok(result);
});

app.MapGet("/debug/articles/recent", async (
    NewsSummarizerDbContext dbContext,
    CancellationToken cancellationToken,
    int limit = 20) =>
{
    var articles = await dbContext.NewsArticles
        .OrderByDescending(article => article.FetchedAt)
        .Take(limit)
        .Select(article => new
        {
            article.Id,
            article.SourceId,
            article.Title,
            article.Url,
            article.CanonicalUrl,
            article.Language,
            article.PublishedAt,
            article.FetchedAt,
            article.NormalizedTitle,
            article.ContentHash,
            article.DedupKey,
            article.Status,
            article.ExpiresAt,
            AiResults = dbContext.ArticleAiResults
                .Where(result => result.ArticleId == article.Id)
                .OrderByDescending(result => result.CreatedAt)
                .Select(result => new
                {
                    result.Id,
                    result.Provider,
                    result.Model,
                    result.PromptVersion,
                    result.Category,
                    result.ImportanceScore,
                    result.UrgencyScore,
                    result.OpportunityScore,
                    result.Summary,
                    result.Reason,
                    result.OpportunityReason,
                    result.DailyDigestCandidate,
                    result.OpportunityDigestCandidate,
                    result.UrgentCandidate,
                    result.Status,
                    result.ErrorMessage,
                    result.CreatedAt
                })
                .ToList()
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(articles);
});

app.MapGet("/debug/digests/recent", async (
    NewsSummarizerDbContext dbContext,
    CancellationToken cancellationToken,
    int limit = 20) =>
{
    var digests = await dbContext.Digests
        .OrderByDescending(digest => digest.CreatedAt)
        .Take(limit)
        .Select(digest => new
        {
            digest.Id,
            digest.UserId,
            digest.DigestType,
            digest.PeriodStart,
            digest.PeriodEnd,
            digest.Status,
            digest.SentAt,
            digest.CreatedAt,
            Items = dbContext.DigestItems
                .Where(item => item.DigestId == digest.Id)
                .OrderBy(item => item.Position)
                .Select(item => new
                {
                    item.Id,
                    item.ArticleId,
                    item.Position,
                    item.TitleSnapshot,
                    item.UrlSnapshot,
                    item.SourceNameSnapshot,
                    item.SummarySnapshot,
                    item.ReasonSnapshot
                })
                .ToList(),
            Notifications = dbContext.Notifications
                .Where(notification => notification.DigestId == digest.Id)
                .OrderByDescending(notification => notification.CreatedAt)
                .Select(notification => new
                {
                    notification.Id,
                    notification.NotificationType,
                    notification.DedupKey,
                    notification.Status,
                    notification.TitleSnapshot,
                    notification.MessageSnapshot,
                    notification.SentAt,
                    notification.CreatedAt
                })
                .ToList()
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(digests);
});

app.MapGet("/debug/sources", async (
    NewsSummarizerDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var sources = await dbContext.NewsSources
        .OrderBy(source => source.Name)
        .Select(source => new
        {
            source.Id,
            source.Name,
            source.SourceType,
            source.Url,
            source.Language,
            source.DefaultCategories,
            source.IsEnabled,
            source.IsFastSource,
            source.FetchIntervalMinutes,
            source.TrustScore,
            source.LastFetchedAt,
            source.LastError
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(sources);
});

app.MapGet("/debug/users", async (
    NewsSummarizerDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var users = await dbContext.Users
        .OrderBy(user => user.CreatedAt)
        .Select(user => new
        {
            user.Id,
            user.TelegramUserId,
            user.Username,
            user.FirstName,
            user.Status,
            Preferences = dbContext.UserPreferences
                .Where(preferences => preferences.UserId == user.Id)
                .Select(preferences => new
                {
                    preferences.EnabledCategories,
                    preferences.UrgentTopics,
                    preferences.ImportantTopicsText,
                    preferences.ExcludedTopicsText,
                    preferences.DailyDigestEnabled,
                    preferences.DailyDigestTime,
                    preferences.OpportunityDigestEnabled,
                    preferences.OpportunityDigestTime,
                    preferences.UrgentNotificationsEnabled,
                    preferences.MaxItemsPerDigest,
                    preferences.Timezone
                })
                .FirstOrDefault()
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(users);
});

app.Run();
'@

Write-Utf8NoBom "docs/debug-api.md" @'
# Debug API

The debug API is used for local MVP pipeline checks.

## Run database

```powershell
docker compose up -d
```

## Apply migrations

```powershell
dotnet ef database update --project src/NewsSummarizer.Infrastructure --startup-project src/NewsSummarizer.Api
```

## Run API

```powershell
dotnet run --project src/NewsSummarizer.Api
```

## Seed data

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/seed
```

## Fetch mock news

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/fetch-news
```

## Analyze pending articles

```powershell
Invoke-RestMethod -Method Post "http://localhost:5000/debug/analyze-articles?limit=20"
```

## Build daily digests

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/build-daily-digests
```

## Build opportunity digests

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/build-opportunity-digests
```

## See recent articles

```powershell
Invoke-RestMethod http://localhost:5000/debug/articles/recent | ConvertTo-Json -Depth 10
```

## See recent digests

```powershell
Invoke-RestMethod http://localhost:5000/debug/digests/recent | ConvertTo-Json -Depth 10
```

## See sources

```powershell
Invoke-RestMethod http://localhost:5000/debug/sources
```

## See users

```powershell
Invoke-RestMethod http://localhost:5000/debug/users
```

## Expected mock flow

```text
POST /debug/seed
POST /debug/fetch-news
POST /debug/analyze-articles
POST /debug/build-daily-digests
POST /debug/build-opportunity-digests
GET  /debug/digests/recent
```
'@

if (-not $SkipBuild) {
    Invoke-Dotnet @("build")
}

Write-Host "Opportunity digest pipeline was created."
Write-Host "Commit suggestion:"
Write-Host "feat(core): add opportunity digest pipeline"
