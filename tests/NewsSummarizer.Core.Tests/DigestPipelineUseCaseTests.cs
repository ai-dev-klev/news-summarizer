using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.Services;
using NewsSummarizer.Core.UseCases;

namespace NewsSummarizer.Core.Tests;

public sealed class DigestPipelineUseCaseTests
{
    private static readonly DateTimeOffset PeriodStart = new(2026, 5, 17, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 5, 18, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task BuildDailyDigest_ShouldCreateDigestItemsAndNotification_ForMatchingCandidates()
    {
        var user = CreateUser();
        var matchingArticle = CreateArticle("Technology article", PeriodStart.AddHours(1));
        var nonCandidateArticle = CreateArticle("Non candidate", PeriodStart.AddHours(2));
        var wrongCategoryArticle = CreateArticle("Sport article", PeriodStart.AddHours(3));

        var matchingResult = CreateAiResult(
            matchingArticle.Id,
            category: "technology",
            importanceScore: 80,
            urgencyScore: 40,
            opportunityScore: 20,
            dailyCandidate: true,
            opportunityCandidate: false);

        var nonCandidateResult = CreateAiResult(
            nonCandidateArticle.Id,
            category: "technology",
            importanceScore: 100,
            urgencyScore: 100,
            opportunityScore: 100,
            dailyCandidate: false,
            opportunityCandidate: false);

        var wrongCategoryResult = CreateAiResult(
            wrongCategoryArticle.Id,
            category: "sport",
            importanceScore: 99,
            urgencyScore: 99,
            opportunityScore: 99,
            dailyCandidate: true,
            opportunityCandidate: false);

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, enabledCategories: ["technology"])],
            articles: [matchingArticle, nonCandidateArticle, wrongCategoryArticle],
            aiResults: [matchingResult, nonCandidateResult, wrongCategoryResult]);

        var useCase = CreateDailyUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.UsersChecked);
        Assert.Equal(1, summary.DigestsCreated);
        Assert.Equal(0, summary.UsersSkippedDisabled);
        Assert.Equal(0, summary.UsersSkippedExistingDigest);
        Assert.Equal(0, summary.UsersSkippedNoItems);

        var digest = Assert.Single(context.DigestRepository.AddedDigests);
        Assert.Equal(user.Id, digest.UserId);
        Assert.Equal(DigestType.Daily, digest.DigestType);
        Assert.Equal(PeriodStart, digest.PeriodStart);
        Assert.Equal(PeriodEnd, digest.PeriodEnd);
        Assert.Equal(DigestStatus.Created, digest.Status);

        var item = Assert.Single(context.DigestRepository.AddedItems);
        Assert.Equal(matchingArticle.Id, item.ArticleId);
        Assert.Equal(1, item.Position);
        Assert.Equal(matchingArticle.Title, item.TitleSnapshot);
        Assert.Equal(matchingArticle.Url, item.UrlSnapshot);
        Assert.Equal(matchingResult.Summary, item.SummarySnapshot);
        Assert.Equal(matchingResult.Reason, item.ReasonSnapshot);

        var notification = Assert.Single(context.NotificationRepository.AddedNotifications);
        Assert.Equal(NotificationType.DailyDigest, notification.NotificationType);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(digest.Id, notification.DigestId);
        Assert.Null(notification.ArticleId);
        Assert.Contains("Ежедневная сводка", notification.TitleSnapshot);
        Assert.Contains(matchingArticle.Title, notification.MessageSnapshot);
        Assert.Contains(matchingArticle.Url, notification.MessageSnapshot);

        Assert.True(context.DigestRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task BuildDailyDigest_ShouldSortByImportanceThenUrgency_AndRespectMaxItems()
    {
        var user = CreateUser();
        var lowImportance = CreateArticle("Low importance", PeriodStart.AddHours(1));
        var highImportanceLowUrgency = CreateArticle("High importance low urgency", PeriodStart.AddHours(2));
        var highImportanceHighUrgency = CreateArticle("High importance high urgency", PeriodStart.AddHours(3));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, enabledCategories: [], maxItemsPerDigest: 2)],
            articles: [lowImportance, highImportanceLowUrgency, highImportanceHighUrgency],
            aiResults:
            [
                CreateAiResult(lowImportance.Id, "general", 60, 100, 10, dailyCandidate: true, opportunityCandidate: false),
                CreateAiResult(highImportanceLowUrgency.Id, "general", 90, 10, 10, dailyCandidate: true, opportunityCandidate: false),
                CreateAiResult(highImportanceHighUrgency.Id, "general", 90, 80, 10, dailyCandidate: true, opportunityCandidate: false)
            ]);

        var useCase = CreateDailyUseCase(context);

        await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Collection(
            context.DigestRepository.AddedItems.OrderBy(item => item.Position),
            first => Assert.Equal(highImportanceHighUrgency.Id, first.ArticleId),
            second => Assert.Equal(highImportanceLowUrgency.Id, second.ArticleId));
    }

    [Fact]
    public async Task BuildDailyDigest_ShouldSkip_WhenDailyDigestDisabled()
    {
        var user = CreateUser();
        var article = CreateArticle("Article", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, dailyEnabled: false)],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, "general", 90, 10, 10, dailyCandidate: true, opportunityCandidate: false)]);

        var useCase = CreateDailyUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.UsersChecked);
        Assert.Equal(0, summary.DigestsCreated);
        Assert.Equal(1, summary.UsersSkippedDisabled);
        Assert.Empty(context.DigestRepository.AddedDigests);
        Assert.Empty(context.NotificationRepository.AddedNotifications);
    }

    [Fact]
    public async Task BuildDailyDigest_ShouldSkip_WhenDigestAlreadyExists()
    {
        var user = CreateUser();
        var article = CreateArticle("Article", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id)],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, "general", 90, 10, 10, dailyCandidate: true, opportunityCandidate: false)]);

        context.DigestRepository.ExistingDigests.Add(new DigestKey(user.Id, DigestType.Daily, PeriodStart, PeriodEnd));

        var useCase = CreateDailyUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.UsersChecked);
        Assert.Equal(0, summary.DigestsCreated);
        Assert.Equal(1, summary.UsersSkippedExistingDigest);
        Assert.Empty(context.DigestRepository.AddedDigests);
        Assert.Empty(context.NotificationRepository.AddedNotifications);
    }

    [Fact]
    public async Task BuildDailyDigest_ShouldSkip_WhenNoItemsMatchUserCategories()
    {
        var user = CreateUser();
        var article = CreateArticle("Business article", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, enabledCategories: ["technology"])],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, "business", 90, 10, 10, dailyCandidate: true, opportunityCandidate: false)]);

        var useCase = CreateDailyUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.UsersChecked);
        Assert.Equal(0, summary.DigestsCreated);
        Assert.Equal(1, summary.UsersSkippedNoItems);
        Assert.Empty(context.DigestRepository.AddedDigests);
        Assert.Empty(context.NotificationRepository.AddedNotifications);
    }

    [Fact]
    public async Task BuildDailyDigest_ShouldTreatEmptyUserCategoriesAsAllCategoriesAllowed()
    {
        var user = CreateUser();
        var article = CreateArticle("Business article", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, enabledCategories: [])],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, "business", 90, 10, 10, dailyCandidate: true, opportunityCandidate: false)]);

        var useCase = CreateDailyUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.DigestsCreated);
        Assert.Single(context.DigestRepository.AddedItems);
    }

    [Fact]
    public async Task BuildDailyDigest_ShouldNotCreateDuplicateNotification_WhenNotificationAlreadyExists()
    {
        var user = CreateUser();
        var article = CreateArticle("Article", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id)],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, "general", 90, 10, 10, dailyCandidate: true, opportunityCandidate: false)]);

        context.NotificationRepository.AlwaysExists = true;

        var useCase = CreateDailyUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.DigestsCreated);
        Assert.Single(context.DigestRepository.AddedDigests);
        Assert.Single(context.DigestRepository.AddedItems);
        Assert.Empty(context.NotificationRepository.AddedNotifications);
    }

    [Fact]
    public async Task BuildOpportunityDigest_ShouldCreateOpportunityDigestAndNotification()
    {
        var user = CreateUser();
        var article = CreateArticle("AI research article", PeriodStart.AddHours(1));

        var aiResult = CreateAiResult(
            article.Id,
            category: "research",
            importanceScore: 40,
            urgencyScore: 10,
            opportunityScore: 95,
            dailyCandidate: false,
            opportunityCandidate: true,
            opportunityReason: "Can become a startup idea.");

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, enabledCategories: ["research"])],
            articles: [article],
            aiResults: [aiResult]);

        var useCase = CreateOpportunityUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.UsersChecked);
        Assert.Equal(1, summary.DigestsCreated);
        Assert.Equal(0, summary.UsersSkippedDisabled);
        Assert.Equal(0, summary.UsersSkippedExistingDigest);
        Assert.Equal(0, summary.UsersSkippedNoItems);

        var digest = Assert.Single(context.DigestRepository.AddedDigests);
        Assert.Equal(DigestType.Opportunity, digest.DigestType);

        var item = Assert.Single(context.DigestRepository.AddedItems);
        Assert.Equal(article.Id, item.ArticleId);
        Assert.Equal(aiResult.OpportunityReason, item.ReasonSnapshot);

        var notification = Assert.Single(context.NotificationRepository.AddedNotifications);
        Assert.Equal(NotificationType.OpportunityDigest, notification.NotificationType);
        Assert.Equal(digest.Id, notification.DigestId);
        Assert.Contains("Сводка возможностей", notification.TitleSnapshot);
        Assert.Contains(article.Title, notification.MessageSnapshot);
        Assert.NotNull(aiResult.OpportunityReason);
        Assert.Contains(aiResult.OpportunityReason!, notification.MessageSnapshot);
    }

    [Fact]
    public async Task BuildOpportunityDigest_ShouldSortByOpportunityThenImportance_AndRespectMaxItems()
    {
        var user = CreateUser();
        var lowerOpportunity = CreateArticle("Lower opportunity", PeriodStart.AddHours(1));
        var highOpportunityLowImportance = CreateArticle("High opportunity low importance", PeriodStart.AddHours(2));
        var highOpportunityHighImportance = CreateArticle("High opportunity high importance", PeriodStart.AddHours(3));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, enabledCategories: [], maxItemsPerDigest: 2)],
            articles: [lowerOpportunity, highOpportunityLowImportance, highOpportunityHighImportance],
            aiResults:
            [
                CreateAiResult(lowerOpportunity.Id, "research", 100, 10, 70, dailyCandidate: false, opportunityCandidate: true),
                CreateAiResult(highOpportunityLowImportance.Id, "research", 10, 10, 95, dailyCandidate: false, opportunityCandidate: true),
                CreateAiResult(highOpportunityHighImportance.Id, "research", 90, 10, 95, dailyCandidate: false, opportunityCandidate: true)
            ]);

        var useCase = CreateOpportunityUseCase(context);

        await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Collection(
            context.DigestRepository.AddedItems.OrderBy(item => item.Position),
            first => Assert.Equal(highOpportunityHighImportance.Id, first.ArticleId),
            second => Assert.Equal(highOpportunityLowImportance.Id, second.ArticleId));
    }

    [Fact]
    public async Task BuildOpportunityDigest_ShouldSkip_WhenOpportunityDigestDisabled()
    {
        var user = CreateUser();
        var article = CreateArticle("Article", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, opportunityEnabled: false)],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, "research", 10, 10, 90, dailyCandidate: false, opportunityCandidate: true)]);

        var useCase = CreateOpportunityUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.UsersChecked);
        Assert.Equal(0, summary.DigestsCreated);
        Assert.Equal(1, summary.UsersSkippedDisabled);
        Assert.Empty(context.DigestRepository.AddedDigests);
        Assert.Empty(context.NotificationRepository.AddedNotifications);
    }

    [Fact]
    public async Task BuildOpportunityDigest_ShouldSkip_WhenDigestAlreadyExists()
    {
        var user = CreateUser();
        var article = CreateArticle("Article", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id)],
            articles: [article],
            aiResults: [CreateAiResult(article.Id, "research", 10, 10, 90, dailyCandidate: false, opportunityCandidate: true)]);

        context.DigestRepository.ExistingDigests.Add(new DigestKey(user.Id, DigestType.Opportunity, PeriodStart, PeriodEnd));

        var useCase = CreateOpportunityUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.UsersChecked);
        Assert.Equal(0, summary.DigestsCreated);
        Assert.Equal(1, summary.UsersSkippedExistingDigest);
        Assert.Empty(context.DigestRepository.AddedDigests);
        Assert.Empty(context.NotificationRepository.AddedNotifications);
    }

    [Fact]
    public async Task BuildOpportunityDigest_ShouldUseReason_WhenOpportunityReasonIsMissing()
    {
        var user = CreateUser();
        var article = CreateArticle("Article", PeriodStart.AddHours(1));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, enabledCategories: [])],
            articles: [article],
            aiResults:
            [
                CreateAiResult(
                    article.Id,
                    category: "research",
                    importanceScore: 10,
                    urgencyScore: 10,
                    opportunityScore: 90,
                    dailyCandidate: false,
                    opportunityCandidate: true,
                    reason: "General reason.",
                    opportunityReason: null)
            ]);

        var useCase = CreateOpportunityUseCase(context);

        await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        var item = Assert.Single(context.DigestRepository.AddedItems);
        Assert.Equal("General reason.", item.ReasonSnapshot);
    }

    [Fact]
    public async Task DigestUseCases_ShouldUseLatestAiResultPerArticle()
    {
        var user = CreateUser();
        var article = CreateArticle("Article", PeriodStart.AddHours(1));
        var olderResult = CreateAiResult(
            article.Id,
            category: "business",
            importanceScore: 100,
            urgencyScore: 100,
            opportunityScore: 100,
            dailyCandidate: true,
            opportunityCandidate: true,
            createdAt: PeriodStart.AddMinutes(1));

        var latestResult = CreateAiResult(
            article.Id,
            category: "technology",
            importanceScore: 90,
            urgencyScore: 10,
            opportunityScore: 10,
            dailyCandidate: true,
            opportunityCandidate: false,
            createdAt: PeriodStart.AddMinutes(2));

        var context = CreateContext(
            users: [user],
            preferences: [CreatePreferences(user.Id, enabledCategories: ["technology"])],
            articles: [article],
            aiResults: [olderResult, latestResult]);

        var useCase = CreateDailyUseCase(context);

        var summary = await useCase.ExecuteAsync(PeriodStart, PeriodEnd, CancellationToken.None);

        Assert.Equal(1, summary.DigestsCreated);
        var item = Assert.Single(context.DigestRepository.AddedItems);
        Assert.Equal(latestResult.Summary, item.SummarySnapshot);
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
            new FakeDigestRepository(),
            new FakeNotificationRepository());
    }

    private static BuildDailyDigestUseCase CreateDailyUseCase(TestContext context)
    {
        return new BuildDailyDigestUseCase(
            context.UserRepository,
            context.PreferencesRepository,
            context.ArticleRepository,
            context.AiResultRepository,
            context.DigestRepository,
            context.NotificationRepository,
            new RetentionPolicyService());
    }

    private static BuildOpportunityDigestUseCase CreateOpportunityUseCase(TestContext context)
    {
        return new BuildOpportunityDigestUseCase(
            context.UserRepository,
            context.PreferencesRepository,
            context.ArticleRepository,
            context.AiResultRepository,
            context.DigestRepository,
            context.NotificationRepository,
            new RetentionPolicyService());
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
        IReadOnlyList<string>? enabledCategories = null,
        bool dailyEnabled = true,
        bool opportunityEnabled = true,
        int maxItemsPerDigest = 10)
    {
        var now = DateTimeOffset.UtcNow;

        return new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EnabledCategories = enabledCategories?.ToList() ?? ["general"],
            DailyDigestEnabled = dailyEnabled,
            OpportunityDigestEnabled = opportunityEnabled,
            UrgentNotificationsEnabled = true,
            MaxItemsPerDigest = maxItemsPerDigest,
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
        string? category,
        int importanceScore,
        int urgencyScore,
        int opportunityScore,
        bool dailyCandidate,
        bool opportunityCandidate,
        string? reason = "Reason",
        string? opportunityReason = "Opportunity reason",
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
            Category = category,
            ImportanceScore = importanceScore,
            UrgencyScore = urgencyScore,
            OpportunityScore = opportunityScore,
            Summary = $"Summary {articleId:N}",
            Reason = reason,
            OpportunityReason = opportunityReason,
            DailyDigestCandidate = dailyCandidate,
            OpportunityDigestCandidate = opportunityCandidate,
            UrgentCandidate = false,
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
        FakeDigestRepository DigestRepository,
        FakeNotificationRepository NotificationRepository);

    private sealed record DigestKey(
        Guid UserId,
        DigestType DigestType,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd);

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

        public Task<NewsArticle?> FindDuplicateAsync(
            ArticleDeduplicationKey key,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<NewsArticle?>(null);
        }

        public Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_articles.FirstOrDefault(article => article.Id == id));
        }

        public Task<IReadOnlyList<NewsArticle>> GetPendingAiAsync(
            int limit,
            CancellationToken cancellationToken)
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
                .OrderBy(article => article.FetchedAt)
                .Take(limit)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<NewsArticle>> GetRecentAsync(
            int limit,
            CancellationToken cancellationToken)
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

        public Task<ArticleAiResult?> GetLatestSuccessfulByArticleIdAsync(
            Guid articleId,
            CancellationToken cancellationToken)
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
                .Where(item => articleIds.Contains(item.ArticleId))
                .Where(item => item.Status == AiResultStatus.Success)
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

    private sealed class FakeDigestRepository : IDigestRepository
    {
        public List<DigestKey> ExistingDigests { get; } = [];
        public List<Digest> AddedDigests { get; } = [];
        public List<DigestItem> AddedItems { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<bool> ExistsAsync(
            Guid userId,
            DigestType digestType,
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd,
            CancellationToken cancellationToken)
        {
            var exists = ExistingDigests.Contains(new DigestKey(userId, digestType, periodStart, periodEnd));

            return Task.FromResult(exists);
        }

        public Task AddAsync(
            Digest digest,
            IReadOnlyCollection<DigestItem> items,
            CancellationToken cancellationToken)
        {
            AddedDigests.Add(digest);
            AddedItems.AddRange(items);

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> AddedNotifications { get; } = [];
        public bool AlwaysExists { get; set; }
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