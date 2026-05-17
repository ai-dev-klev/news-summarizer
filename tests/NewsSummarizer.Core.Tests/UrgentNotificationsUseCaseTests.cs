using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.Services;
using NewsSummarizer.Core.UseCases;

namespace NewsSummarizer.Core.Tests;

public sealed class UrgentNotificationsUseCaseTests
{
    private static readonly DateTimeOffset PeriodStart = new(2026, 5, 17, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 5, 18, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_ShouldCreateUrgentNotification_ForMatchingUrgentCandidate()
    {
        var user = CreateUser();
        var article = CreateArticle("Urgent market crisis", PeriodStart.AddHours(1));
        var aiResult = CreateAiResult(
            article.Id,
            urgentCandidate: true,
            urgencyScore: 95,
            reason: "Market crash risk.");

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, urgentTopics: ["market_crash"])],
            articles: [article],
            aiResults: [aiResult]);

        var useCase = CreateUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.UsersChecked);
        Assert.Equal(1, summary.ArticlesChecked);
        Assert.Equal(1, summary.NotificationsCreated);
        Assert.Equal(0, summary.UsersSkippedDisabled);
        Assert.Equal(0, summary.NotificationsSkippedExisting);
        Assert.Equal(0, summary.ArticlesSkippedByPreferences);

        var notification = Assert.Single(context.NotificationRepository.AddedNotifications);
        Assert.Equal(user.Id, notification.UserId);
        Assert.Equal(article.Id, notification.ArticleId);
        Assert.Null(notification.DigestId);
        Assert.Equal(NotificationType.Urgent, notification.NotificationType);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Contains(article.Title, notification.TitleSnapshot);
        Assert.Contains(article.Title, notification.MessageSnapshot);
        Assert.Contains(article.Url, notification.MessageSnapshot);
        Assert.Contains(aiResult.Reason!, notification.MessageSnapshot);
        Assert.False(string.IsNullOrWhiteSpace(notification.DedupKey));
        Assert.True(notification.ExpiresAt > notification.CreatedAt);
        Assert.True(context.NotificationRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipUser_WhenUrgentNotificationsDisabled()
    {
        var user = CreateUser();
        var article = CreateArticle("Urgent market crisis", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, urgentEnabled: false)],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, urgentCandidate: true, urgencyScore: 95)]);

        var useCase = CreateUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.UsersChecked);
        Assert.Equal(1, summary.UsersSkippedDisabled);
        Assert.Equal(0, summary.NotificationsCreated);
        Assert.Empty(context.NotificationRepository.AddedNotifications);
        Assert.True(context.NotificationRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipArticle_WhenAiResultIsNotUrgentCandidate()
    {
        var user = CreateUser();
        var article = CreateArticle("Normal article", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, urgentTopics: [])],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, urgentCandidate: false, urgencyScore: 100)]);

        var useCase = CreateUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.UsersChecked);
        Assert.Equal(0, summary.ArticlesChecked);
        Assert.Equal(0, summary.NotificationsCreated);
        Assert.Empty(context.NotificationRepository.AddedNotifications);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipArticle_WhenUrgentTopicDoesNotMatchAndUrgencyIsLow()
    {
        var user = CreateUser();
        var article = CreateArticle("Sports update", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, urgentTopics: ["war", "pandemic"])],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, urgentCandidate: true, urgencyScore: 60, reason: "Not critical.")]);

        var useCase = CreateUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.ArticlesChecked);
        Assert.Equal(0, summary.NotificationsCreated);
        Assert.Equal(1, summary.ArticlesSkippedByPreferences);
        Assert.Empty(context.NotificationRepository.AddedNotifications);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAllowHighUrgencyAsFallback_WhenTopicDoesNotMatch()
    {
        var user = CreateUser();
        var article = CreateArticle("Central bank emergency decision", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, urgentTopics: ["war", "pandemic"])],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, urgentCandidate: true, urgencyScore: 95, reason: "Emergency decision.")]);

        var useCase = CreateUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.NotificationsCreated);
        Assert.Single(context.NotificationRepository.AddedNotifications);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCreateDuplicate_WhenNotificationAlreadyExists()
    {
        var user = CreateUser();
        var article = CreateArticle("Urgent market crisis", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, urgentTopics: ["market_crash"])],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, urgentCandidate: true, urgencyScore: 95)]);

        context.NotificationRepository.AlwaysExists = true;

        var useCase = CreateUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(0, summary.NotificationsCreated);
        Assert.Equal(1, summary.NotificationsSkippedExisting);
        Assert.Empty(context.NotificationRepository.AddedNotifications);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseLatestUrgentAiResultPerArticle()
    {
        var user = CreateUser();
        var article = CreateArticle("Urgent market crisis", PeriodStart.AddHours(1));

        var olderResult = CreateAiResult(
            article.Id,
            urgentCandidate: true,
            urgencyScore: 95,
            reason: "Older reason.",
            createdAt: PeriodStart.AddMinutes(1));

        var latestResult = CreateAiResult(
            article.Id,
            urgentCandidate: true,
            urgencyScore: 95,
            reason: "Latest reason.",
            createdAt: PeriodStart.AddMinutes(2));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, urgentTopics: ["market_crash"])],
            articles: [article],
            aiResults: [olderResult, latestResult]);

        var useCase = CreateUseCase(context);

        await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        var notification = Assert.Single(context.NotificationRepository.AddedNotifications);
        Assert.Contains("Latest reason.", notification.MessageSnapshot);
        Assert.DoesNotContain("Older reason.", notification.MessageSnapshot);
    }

    private static SendUrgentNotificationsUseCase CreateUseCase(TestContext context)
    {
        return new SendUrgentNotificationsUseCase(
            context.UserRepository,
            context.PreferencesRepository,
            context.ArticleRepository,
            context.AiResultRepository,
            context.NotificationRepository,
            new RetentionPolicyService());
    }

    private static TestContext CreateContext(
        IReadOnlyList<User> users,
        IReadOnlyList<UserPreferences> preferences,
        IReadOnlyList<NewsArticle> articles,
        IReadOnlyList<ArticleAiResult> aiResults)
    {
        return new TestContext(
            new FakeUserRepository(users),
            new FakeUserPreferencesRepository(preferences),
            new FakeArticleRepository(articles),
            new FakeArticleAiResultRepository(aiResults),
            new FakeNotificationRepository());
    }

    private static User CreateUser()
    {
        var now = DateTimeOffset.UtcNow;

        return new User
        {
            Id = Guid.NewGuid(),
            TelegramUserId = Random.Shared.NextInt64(1, long.MaxValue),
            Username = "test_user",
            FirstName = "Test",
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static UserPreferences CreatePreferences(
        Guid userId,
        IReadOnlyList<string>? urgentTopics = null,
        bool urgentEnabled = true)
    {
        var now = DateTimeOffset.UtcNow;

        return new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EnabledCategories = ["general"],
            UrgentTopics = urgentTopics?.ToList() ?? ["market_crash"],
            DailyDigestEnabled = true,
            OpportunityDigestEnabled = true,
            UrgentNotificationsEnabled = urgentEnabled,
            MaxItemsPerDigest = 10,
            Timezone = "Europe/Moscow",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static NewsArticle CreateArticle(string title, DateTimeOffset fetchedAt)
    {
        return new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            Title = title,
            Url = $"https://example.com/{Guid.NewGuid():N}",
            CanonicalUrl = null,
            Description = $"{title} description",
            Content = $"{title} content",
            Language = "en",
            PublishedAt = fetchedAt,
            FetchedAt = fetchedAt,
            NormalizedTitle = title.ToLowerInvariant(),
            ContentHash = Guid.NewGuid().ToString("N"),
            DedupKey = $"dedup:{Guid.NewGuid():N}",
            Status = ArticleStatus.Analyzed,
            ExpiresAt = fetchedAt.AddDays(14),
            CreatedAt = fetchedAt,
            UpdatedAt = fetchedAt
        };
    }

    private static ArticleAiResult CreateAiResult(
        Guid articleId,
        bool urgentCandidate,
        int urgencyScore,
        string? reason = "Urgent reason.",
        DateTimeOffset? createdAt = null)
    {
        var now = createdAt ?? DateTimeOffset.UtcNow;

        return new ArticleAiResult
        {
            Id = Guid.NewGuid(),
            ArticleId = articleId,
            Provider = AiProviderType.Mock,
            Model = "test-model",
            PromptVersion = "test-v1",
            Category = "general",
            ImportanceScore = 50,
            UrgencyScore = urgencyScore,
            OpportunityScore = 20,
            Summary = "Urgent summary.",
            Reason = reason,
            OpportunityReason = null,
            DailyDigestCandidate = false,
            OpportunityDigestCandidate = false,
            UrgentCandidate = urgentCandidate,
            Status = AiResultStatus.Success,
            RawResponseJson = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private sealed record TestContext(
        FakeUserRepository UserRepository,
        FakeUserPreferencesRepository PreferencesRepository,
        FakeArticleRepository ArticleRepository,
        FakeArticleAiResultRepository AiResultRepository,
        FakeNotificationRepository NotificationRepository);

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users;

        public FakeUserRepository(IReadOnlyList<User> users)
        {
            _users = users.ToList();
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.FirstOrDefault(user => user.Id == id));
        }

        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.FirstOrDefault(user => user.TelegramUserId == telegramUserId));
        }

        public Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<User> result = _users
                .Where(user => user.Status == UserStatus.Active)
                .ToList();

            return Task.FromResult(result);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserPreferencesRepository : IUserPreferencesRepository
    {
        private readonly List<UserPreferences> _preferences;

        public FakeUserPreferencesRepository(IReadOnlyList<UserPreferences> preferences)
        {
            _preferences = preferences.ToList();
        }

        public Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_preferences.FirstOrDefault(preferences => preferences.UserId == userId));
        }

        public Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken)
        {
            _preferences.Add(preferences);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeArticleRepository : IArticleRepository
    {
        private readonly List<NewsArticle> _articles;

        public FakeArticleRepository(IReadOnlyList<NewsArticle> articles)
        {
            _articles = articles.ToList();
        }

        public Task<NewsArticle?> FindDuplicateAsync(ArticleDeduplicationKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult<NewsArticle?>(null);
        }

        public Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_articles.FirstOrDefault(article => article.Id == id));
        }

        public Task<IReadOnlyList<NewsArticle>> GetPendingAiAsync(int limit, CancellationToken cancellationToken)
        {
            IReadOnlyList<NewsArticle> result = _articles
                .Where(article => article.Status == ArticleStatus.PendingAi)
                .Take(limit)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<NewsArticle>> GetAnalyzedForPeriodAsync(
            DateTimeOffset from,
            DateTimeOffset to,
            int limit,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<NewsArticle> result = _articles
                .Where(article => article.Status == ArticleStatus.Analyzed)
                .Where(article => article.FetchedAt >= from && article.FetchedAt < to)
                .Take(limit)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<NewsArticle>> GetRecentAsync(int limit, CancellationToken cancellationToken)
        {
            IReadOnlyList<NewsArticle> result = _articles
                .OrderByDescending(article => article.FetchedAt)
                .Take(limit)
                .ToList();

            return Task.FromResult(result);
        }

        public Task AddAsync(NewsArticle article, CancellationToken cancellationToken)
        {
            _articles.Add(article);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeArticleAiResultRepository : IArticleAiResultRepository
    {
        private readonly List<ArticleAiResult> _results;

        public FakeArticleAiResultRepository(IReadOnlyList<ArticleAiResult> results)
        {
            _results = results.ToList();
        }

        public Task<ArticleAiResult?> GetLatestSuccessfulByArticleIdAsync(Guid articleId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_results
                .Where(result => result.ArticleId == articleId)
                .Where(result => result.Status == AiResultStatus.Success)
                .OrderByDescending(result => result.CreatedAt)
                .FirstOrDefault());
        }

        public Task<IReadOnlyList<ArticleAiResult>> GetSuccessfulByArticleIdsAsync(
            IReadOnlyCollection<Guid> articleIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ArticleAiResult> result = _results
                .Where(result => articleIds.Contains(result.ArticleId))
                .Where(result => result.Status == AiResultStatus.Success)
                .ToList();

            return Task.FromResult(result);
        }

        public Task AddAsync(ArticleAiResult result, CancellationToken cancellationToken)
        {
            _results.Add(result);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public bool AlwaysExists { get; set; }
        public List<Notification> AddedNotifications { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<bool> ExistsAsync(
            Guid userId,
            NotificationType type,
            string dedupKey,
            CancellationToken cancellationToken)
        {
            var exists = AlwaysExists || AddedNotifications.Any(notification =>
                notification.UserId == userId &&
                notification.NotificationType == type &&
                string.Equals(notification.DedupKey, dedupKey, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public Task AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            AddedNotifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}