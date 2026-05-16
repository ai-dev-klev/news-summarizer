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
app.MapPost("/debug/send-urgent-notifications", async (
    SendUrgentNotificationsUseCase useCase,
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

app.MapGet("/debug/notifications/recent", async (
    NewsSummarizerDbContext dbContext,
    CancellationToken cancellationToken,
    int limit = 50) =>
{
    var notifications = await dbContext.Notifications
        .OrderByDescending(notification => notification.CreatedAt)
        .Take(limit)
        .Select(notification => new
        {
            notification.Id,
            notification.UserId,
            notification.ArticleId,
            notification.DigestId,
            notification.NotificationType,
            notification.DedupKey,
            notification.Status,
            notification.TitleSnapshot,
            notification.MessageSnapshot,
            notification.SentAt,
            notification.ExpiresAt,
            notification.ErrorMessage,
            notification.CreatedAt
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(notifications);
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