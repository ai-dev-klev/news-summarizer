using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.Services;
using NewsSummarizer.Core.UseCases;

namespace NewsSummarizer.Core.Tests;

public sealed class FetchNewsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnZeroSummary_WhenNoEnabledSources()
    {
        var sourceRepository = new FakeNewsSourceRepository([]);
        var articleRepository = new FakeArticleRepository();
        var fetcher = new FakeNewsFetcher();

        var useCase = CreateUseCase(sourceRepository, fetcher, articleRepository);

        var summary = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(0, summary.SourcesChecked);
        Assert.Equal(0, summary.ArticlesFetched);
        Assert.Equal(0, summary.ArticlesAdded);
        Assert.Equal(0, summary.DuplicateArticles);
        Assert.Equal(0, summary.SkippedArticles);
        Assert.Equal(0, summary.FailedSources);
        Assert.Empty(articleRepository.AddedArticles);
        Assert.True(articleRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddValidFetchedArticle()
    {
        var source = CreateSource();
        var sourceRepository = new FakeNewsSourceRepository([source]);
        var articleRepository = new FakeArticleRepository();

        var fetcher = new FakeNewsFetcher();
        fetcher.ArticlesBySourceId[source.Id] =
        [
            new FetchedArticle(
                "  AI   Startup Market  ",
                " https://example.com/news?id=42&utm_source=telegram ",
                "  description  ",
                "  content  ",
                "  en  ",
                new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero))
        ];

        var useCase = CreateUseCase(sourceRepository, fetcher, articleRepository);

        var summary = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, summary.SourcesChecked);
        Assert.Equal(1, summary.ArticlesFetched);
        Assert.Equal(1, summary.ArticlesAdded);
        Assert.Equal(0, summary.DuplicateArticles);
        Assert.Equal(0, summary.SkippedArticles);
        Assert.Equal(0, summary.FailedSources);

        var article = Assert.Single(articleRepository.AddedArticles);
        Assert.Equal(source.Id, article.SourceId);
        Assert.Equal("AI   Startup Market", article.Title);
        Assert.Equal("https://example.com/news?id=42&utm_source=telegram", article.Url);
        Assert.Equal("description", article.Description);
        Assert.Equal("content", article.Content);
        Assert.Equal("en", article.Language);
        Assert.Equal(ArticleStatus.PendingAi, article.Status);
        Assert.False(string.IsNullOrWhiteSpace(article.NormalizedTitle));
        Assert.False(string.IsNullOrWhiteSpace(article.DedupKey));
        Assert.True(article.ExpiresAt > article.CreatedAt);
        Assert.True(article.UpdatedAt >= article.CreatedAt);

        Assert.True(articleRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseSourceLanguage_WhenFetchedLanguageIsMissing()
    {
        var source = CreateSource(language: "ru");
        var sourceRepository = new FakeNewsSourceRepository([source]);
        var articleRepository = new FakeArticleRepository();

        var fetcher = new FakeNewsFetcher();
        fetcher.ArticlesBySourceId[source.Id] =
        [
            new FetchedArticle(
                "Title",
                "https://example.com/news",
                null,
                null,
                "  ",
                null)
        ];

        var useCase = CreateUseCase(sourceRepository, fetcher, articleRepository);

        await useCase.ExecuteAsync(CancellationToken.None);

        var article = Assert.Single(articleRepository.AddedArticles);
        Assert.Equal("ru", article.Language);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipArticlesWithoutTitleOrUrl()
    {
        var source = CreateSource();
        var sourceRepository = new FakeNewsSourceRepository([source]);
        var articleRepository = new FakeArticleRepository();

        var fetcher = new FakeNewsFetcher();
        fetcher.ArticlesBySourceId[source.Id] =
        [
            new FetchedArticle("", "https://example.com/empty-title", null, null, "en", null),
            new FetchedArticle("No url", "", null, null, "en", null),
            new FetchedArticle("   ", "https://example.com/whitespace-title", null, null, "en", null),
            new FetchedArticle("Whitespace url", "   ", null, null, "en", null)
        ];

        var useCase = CreateUseCase(sourceRepository, fetcher, articleRepository);

        var summary = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, summary.SourcesChecked);
        Assert.Equal(4, summary.ArticlesFetched);
        Assert.Equal(0, summary.ArticlesAdded);
        Assert.Equal(4, summary.SkippedArticles);
        Assert.Empty(articleRepository.AddedArticles);
        Assert.True(articleRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCountFailedSource_WhenFetcherThrows()
    {
        var source = CreateSource();
        var sourceRepository = new FakeNewsSourceRepository([source]);
        var articleRepository = new FakeArticleRepository();

        var fetcher = new FakeNewsFetcher();
        fetcher.FailingSourceIds.Add(source.Id);

        var useCase = CreateUseCase(sourceRepository, fetcher, articleRepository);

        var summary = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, summary.SourcesChecked);
        Assert.Equal(0, summary.ArticlesFetched);
        Assert.Equal(0, summary.ArticlesAdded);
        Assert.Equal(0, summary.DuplicateArticles);
        Assert.Equal(0, summary.SkippedArticles);
        Assert.Equal(1, summary.FailedSources);
        Assert.Empty(articleRepository.AddedArticles);
        Assert.True(articleRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipRepositoryDuplicates()
    {
        var source = CreateSource();
        var sourceRepository = new FakeNewsSourceRepository([source]);
        var articleRepository = new FakeArticleRepository
        {
            AlwaysDuplicate = true
        };

        var fetcher = new FakeNewsFetcher();
        fetcher.ArticlesBySourceId[source.Id] =
        [
            new FetchedArticle("Title", "https://example.com/news", "desc", "content", "en", null)
        ];

        var useCase = CreateUseCase(sourceRepository, fetcher, articleRepository);

        var summary = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, summary.ArticlesFetched);
        Assert.Equal(0, summary.ArticlesAdded);
        Assert.Equal(1, summary.DuplicateArticles);
        Assert.Empty(articleRepository.AddedArticles);
        Assert.Single(articleRepository.DeduplicationKeys);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipDuplicatesInsideSameRun()
    {
        var source = CreateSource();
        var sourceRepository = new FakeNewsSourceRepository([source]);
        var articleRepository = new FakeArticleRepository();

        var fetcher = new FakeNewsFetcher();
        fetcher.ArticlesBySourceId[source.Id] =
        [
            new FetchedArticle("Same title", "https://example.com/news", "desc", "same content", "en", null),
            new FetchedArticle("Same title", "https://example.com/news", "desc", "same content", "en", null)
        ];

        var useCase = CreateUseCase(sourceRepository, fetcher, articleRepository);

        var summary = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, summary.ArticlesFetched);
        Assert.Equal(1, summary.ArticlesAdded);
        Assert.Equal(1, summary.DuplicateArticles);
        Assert.Single(articleRepository.AddedArticles);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessMultipleSourcesIndependently()
    {
        var firstSource = CreateSource("First source");
        var secondSource = CreateSource("Second source");

        var sourceRepository = new FakeNewsSourceRepository([firstSource, secondSource]);
        var articleRepository = new FakeArticleRepository();

        var fetcher = new FakeNewsFetcher();
        fetcher.ArticlesBySourceId[firstSource.Id] =
        [
            new FetchedArticle("First", "https://example.com/first", "desc", "content", "en", null)
        ];

        fetcher.ArticlesBySourceId[secondSource.Id] =
        [
            new FetchedArticle("Second", "https://example.com/second", "second desc", "second content", "en", null)
        ];

        var useCase = CreateUseCase(sourceRepository, fetcher, articleRepository);

        var summary = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, summary.SourcesChecked);
        Assert.Equal(2, summary.ArticlesFetched);
        Assert.Equal(2, summary.ArticlesAdded);
        Assert.Equal(2, articleRepository.AddedArticles.Count);
    }

    private static FetchNewsUseCase CreateUseCase(
        FakeNewsSourceRepository sourceRepository,
        FakeNewsFetcher fetcher,
        FakeArticleRepository articleRepository)
    {
        return new FetchNewsUseCase(
            sourceRepository,
            fetcher,
            articleRepository,
            new ArticleNormalizationService(),
            new RetentionPolicyService());
    }

    private static NewsSource CreateSource(string name = "Test source", string language = "en")
    {
        var now = DateTimeOffset.UtcNow;

        return new NewsSource
        {
            Id = Guid.NewGuid(),
            Name = name,
            SourceType = SourceType.Mock,
            Url = $"mock://{Guid.NewGuid():N}",
            Language = language,
            DefaultCategories = ["general"],
            IsEnabled = true,
            IsFastSource = false,
            FetchIntervalMinutes = 60,
            TrustScore = 50,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private sealed class FakeNewsSourceRepository : INewsSourceRepository
    {
        private readonly List<NewsSource> _sources;

        public FakeNewsSourceRepository(IReadOnlyList<NewsSource> sources)
        {
            _sources = sources.ToList();
        }

        public Task<IReadOnlyList<NewsSource>> GetEnabledAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<NewsSource> result = _sources
                .Where(source => source.IsEnabled)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<NewsSource>> GetEnabledFastSourcesAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<NewsSource> result = _sources
                .Where(source => source.IsEnabled && source.IsFastSource)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<NewsSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_sources.FirstOrDefault(source => source.Id == id));
        }

        public Task AddAsync(NewsSource source, CancellationToken cancellationToken)
        {
            _sources.Add(source);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNewsFetcher : INewsFetcher
    {
        public Dictionary<Guid, IReadOnlyList<FetchedArticle>> ArticlesBySourceId { get; } = [];
        public HashSet<Guid> FailingSourceIds { get; } = [];

        public Task<IReadOnlyList<FetchedArticle>> FetchAsync(
            NewsSource source,
            CancellationToken cancellationToken)
        {
            if (FailingSourceIds.Contains(source.Id))
            {
                throw new InvalidOperationException("fetch failed");
            }

            return Task.FromResult(
                ArticlesBySourceId.TryGetValue(source.Id, out var articles)
                    ? articles
                    : []);
        }
    }

    private sealed class FakeArticleRepository : IArticleRepository
    {
        public bool AlwaysDuplicate { get; set; }
        public List<NewsArticle> AddedArticles { get; } = [];
        public List<ArticleDeduplicationKey> DeduplicationKeys { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<NewsArticle?> FindDuplicateAsync(
            ArticleDeduplicationKey key,
            CancellationToken cancellationToken)
        {
            DeduplicationKeys.Add(key);

            if (AlwaysDuplicate ||
                AddedArticles.Any(article =>
                    string.Equals(article.Url, key.Url, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(key.CanonicalUrl) &&
                     string.Equals(article.CanonicalUrl, key.CanonicalUrl, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(key.ContentHash) &&
                     string.Equals(article.ContentHash, key.ContentHash, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(key.DedupKey) &&
                     string.Equals(article.DedupKey, key.DedupKey, StringComparison.OrdinalIgnoreCase))))
            {
                return Task.FromResult<NewsArticle?>(new NewsArticle { Id = Guid.NewGuid() });
            }

            return Task.FromResult<NewsArticle?>(null);
        }

        public Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedArticles.FirstOrDefault(article => article.Id == id));
        }

        public Task<IReadOnlyList<NewsArticle>> GetPendingAiAsync(int limit, CancellationToken cancellationToken)
        {
            IReadOnlyList<NewsArticle> result = AddedArticles
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
            IReadOnlyList<NewsArticle> result = AddedArticles
                .Where(article => article.Status == ArticleStatus.Analyzed)
                .Where(article => article.FetchedAt >= from && article.FetchedAt < to)
                .Take(limit)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<NewsArticle>> GetRecentAsync(int limit, CancellationToken cancellationToken)
        {
            IReadOnlyList<NewsArticle> result = AddedArticles
                .OrderByDescending(article => article.FetchedAt)
                .Take(limit)
                .ToList();

            return Task.FromResult(result);
        }

        public Task AddAsync(NewsArticle article, CancellationToken cancellationToken)
        {
            AddedArticles.Add(article);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}