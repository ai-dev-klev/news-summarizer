using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.UseCases;

namespace NewsSummarizer.Core.Tests;

public sealed class AnalyzeArticleUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenLimitIsZero()
    {
        var useCase = CreateUseCase(
            articles: [],
            aiProvider: new SuccessfulFakeAiProvider());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            useCase.ExecuteAsync(0, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDoNothing_WhenNoPendingArticles()
    {
        var articleRepository = new FakeArticleRepository([]);
        var resultRepository = new FakeArticleAiResultRepository();
        var useCase = CreateUseCase(
            articleRepository,
            resultRepository,
            new SuccessfulFakeAiProvider());

        var summary = await useCase.ExecuteAsync(10, CancellationToken.None);

        Assert.Equal(0, summary.ArticlesTaken);
        Assert.Equal(0, summary.ArticlesAnalyzed);
        Assert.Equal(0, summary.ArticlesFailed);
        Assert.Empty(resultRepository.Results);
        Assert.True(articleRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAnalyzePendingArticle_AndSaveSuccessfulAiResult()
    {
        var article = CreatePendingArticle("AI startup market grows");
        var articleRepository = new FakeArticleRepository([article]);
        var resultRepository = new FakeArticleAiResultRepository();

        var useCase = CreateUseCase(
            articleRepository,
            resultRepository,
            new SuccessfulFakeAiProvider());

        var summary = await useCase.ExecuteAsync(10, CancellationToken.None);

        Assert.Equal(1, summary.ArticlesTaken);
        Assert.Equal(1, summary.ArticlesAnalyzed);
        Assert.Equal(0, summary.ArticlesFailed);

        Assert.Equal(ArticleStatus.Analyzed, article.Status);
        Assert.True(article.UpdatedAt > article.CreatedAt);

        var aiResult = Assert.Single(resultRepository.Results);
        Assert.Equal(article.Id, aiResult.ArticleId);
        Assert.Equal(AiProviderType.Mock, aiResult.Provider);
        Assert.Equal("fake-model", aiResult.Model);
        Assert.Equal("test-v1", aiResult.PromptVersion);
        Assert.Equal("technology", aiResult.Category);
        Assert.Equal(80, aiResult.ImportanceScore);
        Assert.Equal(20, aiResult.UrgencyScore);
        Assert.Equal(90, aiResult.OpportunityScore);
        Assert.True(aiResult.DailyDigestCandidate);
        Assert.True(aiResult.OpportunityDigestCandidate);
        Assert.False(aiResult.UrgentCandidate);
        Assert.Equal(AiResultStatus.Success, aiResult.Status);
        Assert.Equal("""{"ok":true}""", aiResult.RawResponseJson);
        Assert.Null(aiResult.ErrorMessage);

        Assert.True(articleRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkArticleFailed_WhenProviderThrows()
    {
        var article = CreatePendingArticle("Broken article");
        var articleRepository = new FakeArticleRepository([article]);
        var resultRepository = new FakeArticleAiResultRepository();

        var useCase = CreateUseCase(
            articleRepository,
            resultRepository,
            new ThrowingFakeAiProvider("provider failed"));

        var summary = await useCase.ExecuteAsync(10, CancellationToken.None);

        Assert.Equal(1, summary.ArticlesTaken);
        Assert.Equal(0, summary.ArticlesAnalyzed);
        Assert.Equal(1, summary.ArticlesFailed);

        Assert.Equal(ArticleStatus.Failed, article.Status);

        var aiResult = Assert.Single(resultRepository.Results);
        Assert.Equal(article.Id, aiResult.ArticleId);
        Assert.Equal(AiProviderType.Mock, aiResult.Provider);
        Assert.Equal("fake-model", aiResult.Model);
        Assert.Equal("test-v1", aiResult.PromptVersion);
        Assert.Equal(0, aiResult.ImportanceScore);
        Assert.Equal(0, aiResult.UrgencyScore);
        Assert.Equal(0, aiResult.OpportunityScore);
        Assert.Equal(AiResultStatus.Failed, aiResult.Status);
        Assert.Contains("provider failed", aiResult.ErrorMessage);
        Assert.Equal("{}", aiResult.RawResponseJson);

        Assert.True(articleRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectLimit()
    {
        var articles = new[]
        {
            CreatePendingArticle("First"),
            CreatePendingArticle("Second"),
            CreatePendingArticle("Third")
        };

        var articleRepository = new FakeArticleRepository(articles);
        var resultRepository = new FakeArticleAiResultRepository();

        var useCase = CreateUseCase(
            articleRepository,
            resultRepository,
            new SuccessfulFakeAiProvider());

        var summary = await useCase.ExecuteAsync(2, CancellationToken.None);

        Assert.Equal(2, summary.ArticlesTaken);
        Assert.Equal(2, summary.ArticlesAnalyzed);
        Assert.Equal(0, summary.ArticlesFailed);
        Assert.Equal(2, resultRepository.Results.Count);

        Assert.Equal(ArticleStatus.Analyzed, articles[0].Status);
        Assert.Equal(ArticleStatus.Analyzed, articles[1].Status);
        Assert.Equal(ArticleStatus.PendingAi, articles[2].Status);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldClampProviderScores()
    {
        var article = CreatePendingArticle("Out of range scores");
        var articleRepository = new FakeArticleRepository([article]);
        var resultRepository = new FakeArticleAiResultRepository();

        var useCase = CreateUseCase(
            articleRepository,
            resultRepository,
            new OutOfRangeScoreFakeAiProvider());

        await useCase.ExecuteAsync(10, CancellationToken.None);

        var aiResult = Assert.Single(resultRepository.Results);
        Assert.Equal(100, aiResult.ImportanceScore);
        Assert.Equal(0, aiResult.UrgencyScore);
        Assert.Equal(100, aiResult.OpportunityScore);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizeEmptyRawJsonToEmptyObject()
    {
        var article = CreatePendingArticle("Empty raw json");
        var articleRepository = new FakeArticleRepository([article]);
        var resultRepository = new FakeArticleAiResultRepository();

        var useCase = CreateUseCase(
            articleRepository,
            resultRepository,
            new EmptyRawJsonFakeAiProvider());

        await useCase.ExecuteAsync(10, CancellationToken.None);

        var aiResult = Assert.Single(resultRepository.Results);
        Assert.Equal("{}", aiResult.RawResponseJson);
    }

    private static AnalyzeArticleUseCase CreateUseCase(
        IReadOnlyList<NewsArticle> articles,
        IAiProvider aiProvider)
    {
        return CreateUseCase(
            new FakeArticleRepository(articles),
            new FakeArticleAiResultRepository(),
            aiProvider);
    }

    private static AnalyzeArticleUseCase CreateUseCase(
        FakeArticleRepository articleRepository,
        FakeArticleAiResultRepository resultRepository,
        IAiProvider aiProvider)
    {
        return new AnalyzeArticleUseCase(
            articleRepository,
            resultRepository,
            aiProvider);
    }

    private static NewsArticle CreatePendingArticle(string title)
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-5);

        return new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            Title = title,
            Url = $"https://example.com/{Guid.NewGuid():N}",
            Language = "en",
            FetchedAt = now,
            NormalizedTitle = title.ToLowerInvariant(),
            Status = ArticleStatus.PendingAi,
            ExpiresAt = now.AddDays(14),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private sealed class FakeArticleRepository : IArticleRepository
    {
        private readonly List<NewsArticle> _articles;

        public FakeArticleRepository(IReadOnlyList<NewsArticle> articles)
        {
            _articles = articles.ToList();
        }

        public bool SaveChangesCalled { get; private set; }

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
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeArticleAiResultRepository : IArticleAiResultRepository
    {
        public List<ArticleAiResult> Results { get; } = [];

        public Task<ArticleAiResult?> GetLatestSuccessfulByArticleIdAsync(
            Guid articleId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Results
                .Where(result => result.ArticleId == articleId && result.Status == AiResultStatus.Success)
                .OrderByDescending(result => result.CreatedAt)
                .FirstOrDefault());
        }

        public Task<IReadOnlyList<ArticleAiResult>> GetSuccessfulByArticleIdsAsync(
            IReadOnlyCollection<Guid> articleIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ArticleAiResult> result = Results
                .Where(item => articleIds.Contains(item.ArticleId))
                .Where(item => item.Status == AiResultStatus.Success)
                .ToList();

            return Task.FromResult(result);
        }

        public Task AddAsync(ArticleAiResult result, CancellationToken cancellationToken)
        {
            Results.Add(result);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessfulFakeAiProvider : IAiProvider, IAiProviderInfo
    {
        public AiProviderType Provider => AiProviderType.Mock;
        public string Model => "fake-model";
        public string PromptVersion => "test-v1";

        public Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(
            NewsArticle article,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ArticleAiAnalysisResult(
                Category: "technology",
                ImportanceScore: 80,
                UrgencyScore: 20,
                OpportunityScore: 90,
                Summary: "Summary",
                Reason: "Reason",
                OpportunityReason: "Opportunity",
                DailyDigestCandidate: true,
                OpportunityDigestCandidate: true,
                UrgentCandidate: false,
                RawResponseJson: """{"ok":true}"""));
        }

        public Task<DetailedAnalysisResult> AnalyzeInDetailAsync(
            NewsArticle article,
            UserPreferences preferences,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new DetailedAnalysisResult("Detailed", "{}"));
        }
    }

    private sealed class ThrowingFakeAiProvider : IAiProvider, IAiProviderInfo
    {
        private readonly string _message;

        public ThrowingFakeAiProvider(string message)
        {
            _message = message;
        }

        public AiProviderType Provider => AiProviderType.Mock;
        public string Model => "fake-model";
        public string PromptVersion => "test-v1";

        public Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(
            NewsArticle article,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(_message);
        }

        public Task<DetailedAnalysisResult> AnalyzeInDetailAsync(
            NewsArticle article,
            UserPreferences preferences,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(_message);
        }
    }

    private sealed class OutOfRangeScoreFakeAiProvider : IAiProvider, IAiProviderInfo
    {
        public AiProviderType Provider => AiProviderType.Mock;
        public string Model => "fake-model";
        public string PromptVersion => "test-v1";

        public Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(
            NewsArticle article,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ArticleAiAnalysisResult(
                Category: "technology",
                ImportanceScore: 150,
                UrgencyScore: -10,
                OpportunityScore: 101,
                Summary: "Summary",
                Reason: "Reason",
                OpportunityReason: "Opportunity",
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
            return Task.FromResult(new DetailedAnalysisResult("Detailed", "{}"));
        }
    }

    private sealed class EmptyRawJsonFakeAiProvider : IAiProvider, IAiProviderInfo
    {
        public AiProviderType Provider => AiProviderType.Mock;
        public string Model => "fake-model";
        public string PromptVersion => "test-v1";

        public Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(
            NewsArticle article,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ArticleAiAnalysisResult(
                Category: "general",
                ImportanceScore: 50,
                UrgencyScore: 10,
                OpportunityScore: 20,
                Summary: "Summary",
                Reason: "Reason",
                OpportunityReason: "Opportunity",
                DailyDigestCandidate: true,
                OpportunityDigestCandidate: false,
                UrgentCandidate: false,
                RawResponseJson: ""));
        }

        public Task<DetailedAnalysisResult> AnalyzeInDetailAsync(
            NewsArticle article,
            UserPreferences preferences,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new DetailedAnalysisResult("Detailed", "{}"));
        }
    }
}