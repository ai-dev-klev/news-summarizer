using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Services;

namespace NewsSummarizer.Core.Tests;

public sealed class DeduplicationServiceTests
{
    [Fact]
    public void BuildKey_ShouldReturnStableArticleKeyFields()
    {
        var service = new DeduplicationService();

        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            Title = "Title",
            Url = "https://example.com/news",
            CanonicalUrl = "https://example.com/news",
            NormalizedTitle = "title",
            ContentHash = "hash",
            DedupKey = "dedup",
            Status = ArticleStatus.PendingAi,
            FetchedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var key = service.BuildKey(article);

        Assert.Equal("https://example.com/news", key.Url);
        Assert.Equal("https://example.com/news", key.CanonicalUrl);
        Assert.Equal("title", key.NormalizedTitle);
        Assert.Equal("hash", key.ContentHash);
        Assert.Equal("dedup", key.DedupKey);
    }

    [Fact]
    public void BuildKey_ShouldPreserveNullOptionalFields()
    {
        var service = new DeduplicationService();

        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            Title = "Title",
            Url = "https://example.com/news",
            CanonicalUrl = null,
            NormalizedTitle = "title",
            ContentHash = null,
            DedupKey = null,
            Status = ArticleStatus.PendingAi,
            FetchedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var key = service.BuildKey(article);

        Assert.Equal("https://example.com/news", key.Url);
        Assert.Null(key.CanonicalUrl);
        Assert.Equal("title", key.NormalizedTitle);
        Assert.Null(key.ContentHash);
        Assert.Null(key.DedupKey);
    }
}