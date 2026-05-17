using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Services;

namespace NewsSummarizer.Core.Tests;

public sealed class ArticleNormalizationServiceTests
{
    private readonly ArticleNormalizationService _service = new();

    [Theory]
    [InlineData("Simple Title", "simple title")]
    [InlineData("  Simple   Title  ", "simple title")]
    [InlineData("AI STARTUP   Market Grows", "ai startup market grows")]
    [InlineData("Mixed CASE News", "mixed case news")]
    public void NormalizeTitle_ShouldTrimLowercaseAndCollapseSpaces(string input, string expected)
    {
        var result = _service.NormalizeTitle(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ComputeContentHash_ShouldReturnNull_WhenContentIsNullOrWhitespace()
    {
        Assert.Null(_service.ComputeContentHash(null));
        Assert.Null(_service.ComputeContentHash(""));
        Assert.Null(_service.ComputeContentHash("   "));
    }

    [Fact]
    public void ComputeContentHash_ShouldBeStableForSameTrimmedContent()
    {
        var first = _service.ComputeContentHash(" sample content ");
        var second = _service.ComputeContentHash("sample content");

        Assert.NotNull(first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void ComputeContentHash_ShouldChange_WhenContentChanges()
    {
        var first = _service.ComputeContentHash("sample content");
        var second = _service.ComputeContentHash("another content");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void BuildCanonicalUrl_ShouldReturnNull_WhenUrlIsNullOrWhitespace()
    {
        Assert.Null(_service.BuildCanonicalUrl(null));
        Assert.Null(_service.BuildCanonicalUrl(""));
        Assert.Null(_service.BuildCanonicalUrl("   "));
    }

    [Fact]
    public void BuildCanonicalUrl_ShouldTrimInvalidUrl_WhenUrlCannotBeParsed()
    {
        var result = _service.BuildCanonicalUrl(" not a valid url ");

        Assert.Equal("not a valid url", result);
    }

    [Fact]
    public void BuildCanonicalUrl_ShouldRemoveFragment()
    {
        var result = _service.BuildCanonicalUrl("https://example.com/news/article#comments");

        Assert.Equal("https://example.com/news/article", result);
    }

    [Fact]
    public void BuildCanonicalUrl_ShouldRemoveKnownTrackingQueryParameters()
    {
        var result = _service.BuildCanonicalUrl(
            "https://example.com/news/article?utm_source=telegram&utm_medium=social&id=42&fbclid=abc&gclid=def&yclid=ghi");

        Assert.Equal("https://example.com/news/article?id=42", result);
    }

    [Fact]
    public void BuildCanonicalUrl_ShouldKeepUsefulQueryParameters()
    {
        var result = _service.BuildCanonicalUrl("https://example.com/search?q=ai&page=2");

        Assert.Equal("https://example.com/search?q=ai&page=2", result);
    }

    [Fact]
    public void BuildKey_ShouldCopyArticleDeduplicationFields()
    {
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            Title = "AI startup market grows",
            Url = "https://example.com/news?id=42&utm_source=x",
            CanonicalUrl = "https://example.com/news?id=42",
            NormalizedTitle = "ai startup market grows",
            ContentHash = "content-hash",
            DedupKey = "dedup-key",
            Status = ArticleStatus.PendingAi,
            FetchedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var key = _service.BuildKey(article);

        Assert.Equal(article.Url, key.Url);
        Assert.Equal(article.CanonicalUrl, key.CanonicalUrl);
        Assert.Equal(article.NormalizedTitle, key.NormalizedTitle);
        Assert.Equal(article.ContentHash, key.ContentHash);
        Assert.Equal(article.DedupKey, key.DedupKey);
    }
}