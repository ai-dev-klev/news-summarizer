using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Infrastructure.Tests;

internal static class InfrastructureTestData
{
    public static DateTimeOffset UtcNow()
    {
        return DateTimeOffset.UtcNow;
    }

    public static User User(long? telegramUserId = null)
    {
        var now = UtcNow();

        return new User
        {
            Id = Guid.NewGuid(),
            TelegramUserId = telegramUserId ?? Random.Shared.NextInt64(1, long.MaxValue),
            Username = "test_user",
            FirstName = "Test",
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static UserPreferences Preferences(Guid userId)
    {
        var now = UtcNow();

        return new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EnabledCategories = ["general", "technology"],
            UrgentTopics = ["market_crash"],
            ImportantTopicsText = "technology, market",
            ExcludedTopicsText = "clickbait",
            DailyDigestEnabled = true,
            DailyDigestTime = new TimeOnly(9, 0),
            OpportunityDigestEnabled = true,
            OpportunityDigestTime = new TimeOnly(18, 0),
            UrgentNotificationsEnabled = true,
            MaxItemsPerDigest = 10,
            Timezone = "Europe/Moscow",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static NewsSource Source(string? url = null, bool enabled = true, bool fast = false)
    {
        var now = UtcNow();

        return new NewsSource
        {
            Id = Guid.NewGuid(),
            Name = $"Source {Guid.NewGuid():N}",
            SourceType = SourceType.Rss,
            Url = url ?? $"https://example.com/rss/{Guid.NewGuid():N}.xml",
            Language = "en",
            DefaultCategories = ["general", "technology"],
            IsEnabled = enabled,
            IsFastSource = fast,
            FetchIntervalMinutes = 60,
            TrustScore = 70,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static NewsArticle Article(
        Guid sourceId,
        string? url = null,
        string? canonicalUrl = null,
        string? normalizedTitle = null,
        string? contentHash = null,
        string? dedupKey = null,
        ArticleStatus status = ArticleStatus.Analyzed,
        DateTimeOffset? publishedAt = null,
        DateTimeOffset? fetchedAt = null,
        DateTimeOffset? expiresAt = null)
    {
        var now = UtcNow();
        var title = $"Article {Guid.NewGuid():N}";

        return new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            Title = title,
            Url = url ?? $"https://example.com/news/{Guid.NewGuid():N}",
            CanonicalUrl = canonicalUrl,
            Description = "Description",
            Content = "Content",
            Language = "en",
            PublishedAt = publishedAt,
            FetchedAt = fetchedAt ?? now,
            NormalizedTitle = normalizedTitle ?? title.ToLowerInvariant(),
            ContentHash = contentHash,
            DedupKey = dedupKey,
            Status = status,
            ExpiresAt = expiresAt ?? now.AddDays(14),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static ArticleAiResult AiResult(
        Guid articleId,
        AiResultStatus status = AiResultStatus.Success,
        AiProviderType provider = AiProviderType.Mock,
        string promptVersion = "test-v1",
        int importanceScore = 70,
        int urgencyScore = 20,
        int opportunityScore = 30,
        DateTimeOffset? createdAt = null)
    {
        var now = createdAt ?? UtcNow();

        return new ArticleAiResult
        {
            Id = Guid.NewGuid(),
            ArticleId = articleId,
            Provider = provider,
            Model = "test-model",
            PromptVersion = promptVersion,
            Category = "technology",
            ImportanceScore = importanceScore,
            UrgencyScore = urgencyScore,
            OpportunityScore = opportunityScore,
            Summary = "Summary",
            Reason = "Reason",
            OpportunityReason = "Opportunity",
            DailyDigestCandidate = true,
            OpportunityDigestCandidate = true,
            UrgentCandidate = false,
            Status = status,
            RawResponseJson = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static Digest Digest(Guid userId, DigestType digestType = DigestType.Daily)
    {
        var now = UtcNow();

        return new Digest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DigestType = digestType,
            PeriodStart = new DateTimeOffset(2026, 5, 17, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero),
            Status = DigestStatus.Created,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static DigestItem DigestItem(Guid digestId, Guid? articleId, int position = 1)
    {
        return new DigestItem
        {
            Id = Guid.NewGuid(),
            DigestId = digestId,
            ArticleId = articleId,
            Position = position,
            TitleSnapshot = "Title snapshot",
            UrlSnapshot = "https://example.com/news",
            SourceNameSnapshot = "Test source",
            SummarySnapshot = "Summary",
            ReasonSnapshot = "Reason",
            CreatedAt = UtcNow()
        };
    }

    public static Notification Notification(
        Guid userId,
        Guid? articleId = null,
        Guid? digestId = null,
        NotificationType notificationType = NotificationType.Urgent,
        string? dedupKey = null,
        DateTimeOffset? expiresAt = null)
    {
        var now = UtcNow();

        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ArticleId = articleId,
            DigestId = digestId,
            NotificationType = notificationType,
            DedupKey = dedupKey ?? $"notification:{Guid.NewGuid():N}",
            Status = NotificationStatus.Pending,
            TitleSnapshot = "Notification title",
            MessageSnapshot = "Notification message",
            ExpiresAt = expiresAt ?? now.AddDays(30),
            CreatedAt = now
        };
    }

    public static DetailedAnalysis DetailedAnalysis(
        Guid userId,
        Guid? articleId = null,
        DateTimeOffset? expiresAt = null)
    {
        var now = UtcNow();

        return new DetailedAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ArticleId = articleId,
            Provider = AiProviderType.Mock,
            Model = "test-model",
            PromptVersion = "test-v1",
            AnalysisText = "Detailed analysis",
            RawResponseJson = "{}",
            Status = AiResultStatus.Success,
            CreatedAt = now,
            ExpiresAt = expiresAt ?? now.AddDays(30)
        };
    }
}