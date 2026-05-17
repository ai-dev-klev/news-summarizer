using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.Services;
using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Infrastructure.Repositories;

namespace NewsSummarizer.Infrastructure.Tests;

[Collection(InfrastructureDatabaseCollection.CollectionName)]
public sealed class EmbeddingPipelineIntegrationTests
{
    private readonly InfrastructureDatabaseFixture _fixture;

    public EmbeddingPipelineIntegrationTests(InfrastructureDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FullPipeline_ShouldSemanticDeduplicateSecondArticle_AndCreateOnlyOneDigestAndUrgentNotification()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var now = DateTimeOffset.UtcNow;
        var periodStart = now.AddHours(-2);
        var periodEnd = now.AddHours(2);

        var user = CreateUser(now);
        var preferences = CreatePreferences(user.Id, now);
        var source = CreateSource(now);

        await context.Users.AddAsync(user);
        await context.UserPreferences.AddAsync(preferences);
        await context.NewsSources.AddAsync(source);
        await context.SaveChangesAsync();

        var fetchedArticles = new[]
        {
            new FetchedArticle(
                Title: "Central bank raises key rate after market crisis",
                Url: "https://example.com/news/central-bank-raises-key-rate",
                Description: "The central bank raised the key rate after market pressure.",
                Content: "The central bank raised the key rate. Analysts expect credit conditions to tighten after the market crisis.",
                Language: "en",
                PublishedAt: now.AddMinutes(-30)),

            new FetchedArticle(
                Title: "Regulator increases interest rate amid financial turmoil",
                Url: "https://example.com/news/regulator-increases-interest-rate",
                Description: "The financial regulator increased the interest rate amid market turmoil.",
                Content: "The regulator increased the interest rate as markets reacted to the same crisis. Borrowing may become more expensive.",
                Language: "en",
                PublishedAt: now.AddMinutes(-20))
        };

        var fetchSummary = await CreateFetchUseCase(
                context,
                new StaticNewsFetcher(fetchedArticles))
            .ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, fetchSummary.SourcesChecked);
        Assert.Equal(2, fetchSummary.ArticlesFetched);
        Assert.Equal(2, fetchSummary.ArticlesAdded);
        Assert.Equal(0, fetchSummary.DuplicateArticles);

        var semanticDeduplication = new SemanticArticleDuplicateDetector(
            new SimilarFinanceStoryEmbeddingProvider(),
            new ArticleEmbeddingRepository(context),
            new SemanticDeduplicationOptions
            {
                Enabled = true,
                LookbackHours = 48,
                RecentCandidateLimit = 20,
                DuplicateThreshold = 0.92,
                MinTextLength = 1
            });

        var analyzeSummary = await CreateAnalyzeUseCase(
                context,
                new AlwaysUrgentBusinessAiProvider(),
                semanticDeduplication)
            .ExecuteAsync(limit: 10, CancellationToken.None);

        Assert.Equal(2, analyzeSummary.ArticlesTaken);
        Assert.Equal(2, analyzeSummary.ArticlesAnalyzed);
        Assert.Equal(0, analyzeSummary.ArticlesFailed);

        var articles = await context.NewsArticles
            .OrderBy(article => article.PublishedAt ?? article.FetchedAt)
            .ToListAsync();

        var statusDebug = string.Join(
            " | ",
            articles.Select(article => $"{article.Title}: {article.Status}, duplicateOf={article.DuplicateOfArticleId}"));

        Assert.True(
            articles.Any(article => article.Status == ArticleStatus.Duplicate),
            "Expected one semantic duplicate. Actual articles: " + statusDebug);

        var analyzedArticle = Assert.Single(articles, article => article.Status == ArticleStatus.Analyzed);
        var duplicateArticle = Assert.Single(articles, article => article.Status == ArticleStatus.Duplicate);

        Assert.NotEqual(analyzedArticle.Id, duplicateArticle.Id);
        Assert.Equal(analyzedArticle.Id, duplicateArticle.DuplicateOfArticleId);

        Assert.Equal(2, await context.ArticleAiResults.CountAsync());
        Assert.Equal(2, await context.ArticleEmbeddings.CountAsync());

        var storedEmbeddings = await context.ArticleEmbeddings
            .OrderBy(embedding => embedding.CreatedAt)
            .ToListAsync();

        Assert.All(storedEmbeddings, embedding =>
        {
            Assert.Equal(AiProviderType.Yandex, embedding.Provider);
            Assert.Equal("fake-yandex-embeddings", embedding.Model);
            Assert.Equal(4, embedding.Dimensions);
            Assert.Equal(4, embedding.Embedding.Length);
            Assert.False(string.IsNullOrWhiteSpace(embedding.TextHash));
        });

        var dailyDigestSummary = await CreateDailyDigestUseCase(context)
            .ExecuteAsync(periodStart, periodEnd, CancellationToken.None);

        Assert.Equal(1, dailyDigestSummary.UsersChecked);
        Assert.Equal(1, dailyDigestSummary.DigestsCreated);
        Assert.Equal(0, dailyDigestSummary.UsersSkippedNoItems);

        var digestItem = await context.DigestItems.SingleAsync();
        Assert.Equal(analyzedArticle.Id, digestItem.ArticleId);
        Assert.NotEqual(duplicateArticle.Id, digestItem.ArticleId);

        var dailyDigestNotification = await context.Notifications
            .SingleAsync(notification => notification.NotificationType == NotificationType.DailyDigest);

        Assert.Equal(NotificationStatus.Pending, dailyDigestNotification.Status);
        Assert.Contains(analyzedArticle.Title, dailyDigestNotification.MessageSnapshot);

        var urgentSummary = await CreateUrgentNotificationsUseCase(context)
            .ExecuteAsync(periodStart, periodEnd, CancellationToken.None);

        Assert.Equal(1, urgentSummary.UsersChecked);
        Assert.Equal(1, urgentSummary.ArticlesChecked);
        Assert.Equal(1, urgentSummary.NotificationsCreated);
        Assert.Equal(0, urgentSummary.NotificationsSkippedExisting);

        var urgentNotification = await context.Notifications
            .SingleAsync(notification => notification.NotificationType == NotificationType.Urgent);

        Assert.Equal(analyzedArticle.Id, urgentNotification.ArticleId);
        Assert.NotEqual(duplicateArticle.Id, urgentNotification.ArticleId);
        Assert.Contains(analyzedArticle.Title, urgentNotification.MessageSnapshot);
    }

    [Fact]
    public async Task AnalyzeArticle_ShouldKeepArticleAnalyzed_WhenEmbeddingProviderFails()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var now = DateTimeOffset.UtcNow;
        var source = CreateSource(now);
        var article = CreatePendingArticle(source.Id, now);

        await context.NewsSources.AddAsync(source);
        await context.NewsArticles.AddAsync(article);
        await context.SaveChangesAsync();

        var semanticDeduplication = new SemanticArticleDuplicateDetector(
            new ThrowingEmbeddingProvider(),
            new ArticleEmbeddingRepository(context),
            new SemanticDeduplicationOptions
            {
                Enabled = true,
                LookbackHours = 48,
                RecentCandidateLimit = 20,
                DuplicateThreshold = 0.92,
                MinTextLength = 1
            });

        var summary = await CreateAnalyzeUseCase(
                context,
                new AlwaysUrgentBusinessAiProvider(),
                semanticDeduplication)
            .ExecuteAsync(limit: 10, CancellationToken.None);

        Assert.Equal(1, summary.ArticlesTaken);
        Assert.Equal(1, summary.ArticlesAnalyzed);
        Assert.Equal(0, summary.ArticlesFailed);

        var loadedArticle = await context.NewsArticles.SingleAsync();
        Assert.Equal(ArticleStatus.Analyzed, loadedArticle.Status);
        Assert.Null(loadedArticle.DuplicateOfArticleId);

        var aiResult = await context.ArticleAiResults.SingleAsync();
        Assert.Equal(AiResultStatus.Success, aiResult.Status);

        Assert.Equal(0, await context.ArticleEmbeddings.CountAsync());
    }

    private static FetchNewsUseCase CreateFetchUseCase(
        NewsSummarizer.Infrastructure.Persistence.NewsSummarizerDbContext context,
        INewsFetcher newsFetcher)
    {
        return new FetchNewsUseCase(
            new NewsSourceRepository(context),
            newsFetcher,
            new ArticleRepository(context),
            new ArticleNormalizationService(),
            new RetentionPolicyService());
    }

    private static AnalyzeArticleUseCase CreateAnalyzeUseCase(
        NewsSummarizer.Infrastructure.Persistence.NewsSummarizerDbContext context,
        IAiProvider aiProvider,
        ISemanticArticleDuplicateDetector semanticDuplicateDetector)
    {
        return new AnalyzeArticleUseCase(
            new ArticleRepository(context),
            new ArticleAiResultRepository(context),
            aiProvider,
            semanticDuplicateDetector);
    }

    private static BuildDailyDigestUseCase CreateDailyDigestUseCase(
        NewsSummarizer.Infrastructure.Persistence.NewsSummarizerDbContext context)
    {
        return new BuildDailyDigestUseCase(
            new UserRepository(context),
            new UserPreferencesRepository(context),
            new ArticleRepository(context),
            new ArticleAiResultRepository(context),
            new DigestRepository(context),
            new NotificationRepository(context),
            new RetentionPolicyService());
    }

    private static SendUrgentNotificationsUseCase CreateUrgentNotificationsUseCase(
        NewsSummarizer.Infrastructure.Persistence.NewsSummarizerDbContext context)
    {
        return new SendUrgentNotificationsUseCase(
            new UserRepository(context),
            new UserPreferencesRepository(context),
            new ArticleRepository(context),
            new ArticleAiResultRepository(context),
            new NotificationRepository(context),
            new RetentionPolicyService());
    }

    private static User CreateUser(DateTimeOffset now)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            TelegramUserId = 424242,
            Username = "embedding_test_user",
            FirstName = "Embedding",
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static UserPreferences CreatePreferences(Guid userId, DateTimeOffset now)
    {
        return new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EnabledCategories = ["business"],
            UrgentTopics = ["market_crash"],
            ImportantTopicsText = "business, finance, market, central bank",
            ExcludedTopicsText = null,
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

    private static NewsSource CreateSource(DateTimeOffset now)
    {
        return new NewsSource
        {
            Id = Guid.NewGuid(),
            Name = "Embedding test mock source",
            SourceType = SourceType.Mock,
            Url = "mock://embedding-pipeline-test",
            Language = "en",
            DefaultCategories = ["business"],
            IsEnabled = true,
            IsFastSource = true,
            FetchIntervalMinutes = 15,
            TrustScore = 90,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static NewsArticle CreatePendingArticle(Guid sourceId, DateTimeOffset now)
    {
        return new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            Title = "Central bank raises key rate after market crisis",
            Url = "https://example.com/news/single-central-bank-rate",
            CanonicalUrl = "https://example.com/news/single-central-bank-rate",
            Description = "The central bank raised the key rate after market pressure.",
            Content = "The central bank raised the key rate. Analysts expect credit conditions to tighten after the market crisis.",
            Language = "en",
            PublishedAt = now.AddMinutes(-10),
            FetchedAt = now,
            NormalizedTitle = "central bank raises key rate after market crisis",
            ContentHash = "single-test-hash",
            DedupKey = "article:single-test",
            Status = ArticleStatus.PendingAi,
            ExpiresAt = now.AddDays(14),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private sealed class StaticNewsFetcher : INewsFetcher
    {
        private readonly IReadOnlyList<FetchedArticle> _articles;

        public StaticNewsFetcher(IReadOnlyList<FetchedArticle> articles)
        {
            _articles = articles;
        }

        public Task<IReadOnlyList<FetchedArticle>> FetchAsync(
            NewsSource source,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_articles);
        }
    }

    private sealed class AlwaysUrgentBusinessAiProvider : IAiProvider, IAiProviderInfo
    {
        public AiProviderType Provider => AiProviderType.Mock;
        public string Model => "fake-ai";
        public string PromptVersion => "embedding-pipeline-test-v1";

        public Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(
            NewsArticle article,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ArticleAiAnalysisResult(
                Category: "business",
                ImportanceScore: 90,
                UrgencyScore: 95,
                OpportunityScore: 70,
                Summary: $"Summary for {article.Title}",
                Reason: "This is urgent because it concerns a market crisis and credit conditions.",
                OpportunityReason: "Useful for deeper business and risk analysis.",
                DailyDigestCandidate: true,
                OpportunityDigestCandidate: true,
                UrgentCandidate: true,
                RawResponseJson: "{}"));
        }

        public Task<DetailedAnalysisResult> AnalyzeInDetailAsync(
            NewsArticle article,
            UserPreferences preferences,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new DetailedAnalysisResult(
                $"Detailed analysis for {article.Title}",
                "{}"));
        }
    }

    private sealed class SimilarFinanceStoryEmbeddingProvider : IEmbeddingProvider
    {
        public bool IsEnabled => true;
        public AiProviderType Provider => AiProviderType.Yandex;
        public string Model => "fake-yandex-embeddings";

        public Task<EmbeddingResult> CreateEmbeddingAsync(
            string input,
            CancellationToken cancellationToken = default)
        {
            var normalized = input.ToLowerInvariant();

            float[] vector =
                normalized.Contains("central bank") ||
                normalized.Contains("regulator") ||
                normalized.Contains("key rate") ||
                normalized.Contains("interest rate")
                    ? [1.0f, 0.0f, 0.0f, 0.0f]
                    : [0.0f, 1.0f, 0.0f, 0.0f];

            return Task.FromResult(new EmbeddingResult(
                Provider,
                Model,
                vector));
        }
    }

    private sealed class ThrowingEmbeddingProvider : IEmbeddingProvider
    {
        public bool IsEnabled => true;
        public AiProviderType Provider => AiProviderType.Yandex;
        public string Model => "throwing-fake-yandex-embeddings";

        public Task<EmbeddingResult> CreateEmbeddingAsync(
            string input,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Synthetic embedding failure for integration test.");
        }
    }
}
