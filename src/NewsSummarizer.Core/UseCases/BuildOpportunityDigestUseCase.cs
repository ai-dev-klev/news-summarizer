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
        var periodStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var periodEnd = periodStart.AddDays(1);

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
                .ThenByDescending(item => item.Article.PublishedAt ?? item.Article.FetchedAt)
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

        var articleCategory = NormalizeCategory(aiResult.Category);

        return preferences.EnabledCategories
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Select(NormalizeCategory)
            .Any(category => string.Equals(category, articleCategory, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeCategory(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();

        return normalized switch
        {
            "sport" => "sports",
            "startup" => "startups",
            "tech" => "technology",
            "ai" => "technology",
            _ => normalized
        };
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