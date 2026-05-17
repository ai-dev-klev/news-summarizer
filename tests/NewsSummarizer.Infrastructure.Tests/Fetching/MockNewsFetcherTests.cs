using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Infrastructure.Fetching;

namespace NewsSummarizer.Infrastructure.Tests.Fetching;

public sealed class MockNewsFetcherTests
{
    [Fact]
    public async Task FetchAsync_ShouldReturnArticles_WhenSourceTypeIsMock()
    {
        var fetcher = new MockNewsFetcher();
        var source = CreateSource(SourceType.Mock);

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.All(result, article =>
        {
            Assert.False(string.IsNullOrWhiteSpace(article.Title));
            Assert.False(string.IsNullOrWhiteSpace(article.Url));
            Assert.False(string.IsNullOrWhiteSpace(article.Language));
            Assert.NotNull(article.PublishedAt);
        });
    }

    [Fact]
    public async Task FetchAsync_ShouldReturnEmpty_WhenSourceTypeIsNotMock()
    {
        var fetcher = new MockNewsFetcher();
        var source = CreateSource(SourceType.Rss);

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchAsync_ShouldGenerateStableMvpScenarioArticles()
    {
        var fetcher = new MockNewsFetcher();
        var source = CreateSource(SourceType.Mock);

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        Assert.Contains(result, article =>
            article.Title.Contains("general", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(result, article =>
            article.Title.Contains("startup", StringComparison.OrdinalIgnoreCase) ||
            article.Title.Contains("opportunity", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(result, article =>
            article.Title.Contains("urgent", StringComparison.OrdinalIgnoreCase) ||
            article.Title.Contains("crisis", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FetchAsync_ShouldRespectCancellationTokenSignature()
    {
        var fetcher = new MockNewsFetcher();
        var source = CreateSource(SourceType.Mock);

        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await fetcher.FetchAsync(source, cancellationTokenSource.Token);

        Assert.NotNull(result);
    }

    private static NewsSource CreateSource(SourceType sourceType)
    {
        var now = DateTimeOffset.UtcNow;

        return new NewsSource
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            SourceType = sourceType,
            Url = sourceType == SourceType.Mock
                ? "mock://test"
                : "https://example.com/feed.xml",
            Language = "en",
            DefaultCategories = ["general"],
            IsEnabled = true,
            IsFastSource = false,
            FetchIntervalMinutes = 60,
            TrustScore = 70,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}