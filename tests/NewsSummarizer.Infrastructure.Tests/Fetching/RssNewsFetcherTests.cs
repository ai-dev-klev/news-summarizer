using System.Net;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Infrastructure.Fetching;

namespace NewsSummarizer.Infrastructure.Tests.Fetching;

public sealed class RssNewsFetcherTests
{
    [Fact]
    public async Task FetchAsync_ShouldReturnEmpty_WhenSourceTypeIsNotRss()
    {
        var fetcher = CreateFetcher("<rss><channel><item><title>Title</title><link>https://example.com/news</link></item></channel></rss>");
        var source = CreateSource(SourceType.Mock);

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchAsync_ShouldParseRssItems()
    {
        var xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">
          <channel>
            <title>Test feed</title>
            <item>
              <title> First RSS title </title>
              <link> https://example.com/news/1 </link>
              <description> First description </description>
              <content:encoded> First full content </content:encoded>
              <pubDate>Sun, 17 May 2026 12:30:00 +0300</pubDate>
            </item>
            <item>
              <title>Second RSS title</title>
              <link>https://example.com/news/2</link>
              <description>Second description</description>
              <pubDate>Sun, 17 May 2026 10:00:00 GMT</pubDate>
            </item>
          </channel>
        </rss>
        """;

        var fetcher = CreateFetcher(xml);
        var source = CreateSource(language: "en");

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal("First RSS title", first.Title);
                Assert.Equal("https://example.com/news/1", first.Url);
                Assert.Equal("First description", first.Description);
                Assert.Equal("First full content", first.Content);
                Assert.Equal("en", first.Language);
                Assert.NotNull(first.PublishedAt);
                Assert.Equal(TimeSpan.Zero, first.PublishedAt!.Value.Offset);
                Assert.Equal(new DateTimeOffset(2026, 5, 17, 9, 30, 0, TimeSpan.Zero), first.PublishedAt);
            },
            second =>
            {
                Assert.Equal("Second RSS title", second.Title);
                Assert.Equal("https://example.com/news/2", second.Url);
                Assert.Equal("Second description", second.Description);
                Assert.Equal("Second description", second.Content);
                Assert.Equal("en", second.Language);
                Assert.NotNull(second.PublishedAt);
                Assert.Equal(TimeSpan.Zero, second.PublishedAt!.Value.Offset);
                Assert.Equal(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero), second.PublishedAt);
            });
    }

    [Fact]
    public async Task FetchAsync_ShouldSkipRssItemsWithoutTitleOrLink()
    {
        var xml = """
        <rss version="2.0">
          <channel>
            <item>
              <title></title>
              <link>https://example.com/no-title</link>
            </item>
            <item>
              <title>Whitespace title</title>
              <link>   </link>
            </item>
            <item>
              <description>No title and no link</description>
            </item>
            <item>
              <title>Valid item</title>
              <link>https://example.com/valid</link>
            </item>
          </channel>
        </rss>
        """;

        var fetcher = CreateFetcher(xml);
        var source = CreateSource();

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        var article = Assert.Single(result);
        Assert.Equal("Valid item", article.Title);
        Assert.Equal("https://example.com/valid", article.Url);
    }

    [Fact]
    public async Task FetchAsync_ShouldSetNullPublishedAt_WhenRssPubDateIsMissingOrInvalid()
    {
        var xml = """
        <rss version="2.0">
          <channel>
            <item>
              <title>No date</title>
              <link>https://example.com/no-date</link>
            </item>
            <item>
              <title>Invalid date</title>
              <link>https://example.com/invalid-date</link>
              <pubDate>not a date</pubDate>
            </item>
          </channel>
        </rss>
        """;

        var fetcher = CreateFetcher(xml);
        var source = CreateSource();

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, article => Assert.Null(article.PublishedAt));
    }

    [Fact]
    public async Task FetchAsync_ShouldPreserveNullDescriptionAndContent_WhenRssItemHasNoTextFields()
    {
        var xml = """
        <rss version="2.0">
          <channel>
            <item>
              <title>Title</title>
              <link>https://example.com/news</link>
            </item>
          </channel>
        </rss>
        """;

        var fetcher = CreateFetcher(xml);
        var source = CreateSource();

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        var article = Assert.Single(result);
        Assert.Null(article.Description);
        Assert.Null(article.Content);
    }

    [Fact]
    public async Task FetchAsync_ShouldParseAtomEntries()
    {
        var xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <feed xmlns="http://www.w3.org/2005/Atom">
          <title>Atom feed</title>
          <entry>
            <title> First Atom title </title>
            <link rel="self" href="https://example.com/feed-entry-self" />
            <link rel="alternate" href="https://example.com/atom/1" />
            <summary> First Atom summary </summary>
            <content> First Atom content </content>
            <published>2026-05-17T12:30:00+03:00</published>
          </entry>
          <entry>
            <title>Second Atom title</title>
            <link href="https://example.com/atom/2" />
            <summary>Second Atom summary</summary>
            <updated>2026-05-17T10:00:00Z</updated>
          </entry>
        </feed>
        """;

        var fetcher = CreateFetcher(xml);
        var source = CreateSource(language: "en");

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal("First Atom title", first.Title);
                Assert.Equal("https://example.com/atom/1", first.Url);
                Assert.Equal("First Atom summary", first.Description);
                Assert.Equal("First Atom content", first.Content);
                Assert.Equal("en", first.Language);
                Assert.NotNull(first.PublishedAt);
                Assert.Equal(TimeSpan.Zero, first.PublishedAt!.Value.Offset);
                Assert.Equal(new DateTimeOffset(2026, 5, 17, 9, 30, 0, TimeSpan.Zero), first.PublishedAt);
            },
            second =>
            {
                Assert.Equal("Second Atom title", second.Title);
                Assert.Equal("https://example.com/atom/2", second.Url);
                Assert.Equal("Second Atom summary", second.Description);
                Assert.Equal("Second Atom summary", second.Content);
                Assert.Equal("en", second.Language);
                Assert.NotNull(second.PublishedAt);
                Assert.Equal(TimeSpan.Zero, second.PublishedAt!.Value.Offset);
                Assert.Equal(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero), second.PublishedAt);
            });
    }

    [Fact]
    public async Task FetchAsync_ShouldSkipAtomEntriesWithoutUsableLink()
    {
        var xml = """
        <feed xmlns="http://www.w3.org/2005/Atom">
          <entry>
            <title>Only self link</title>
            <link rel="self" href="https://example.com/self" />
          </entry>
          <entry>
            <title>Missing link</title>
          </entry>
          <entry>
            <title>Valid Atom item</title>
            <link rel="alternate" href="https://example.com/valid" />
          </entry>
        </feed>
        """;

        var fetcher = CreateFetcher(xml);
        var source = CreateSource();

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        var article = Assert.Single(result);
        Assert.Equal("Valid Atom item", article.Title);
        Assert.Equal("https://example.com/valid", article.Url);
    }

    [Fact]
    public async Task FetchAsync_ShouldSetNullPublishedAt_WhenAtomDateIsInvalid()
    {
        var xml = """
        <feed xmlns="http://www.w3.org/2005/Atom">
          <entry>
            <title>Invalid Atom date</title>
            <link rel="alternate" href="https://example.com/invalid-date" />
            <published>not a date</published>
          </entry>
        </feed>
        """;

        var fetcher = CreateFetcher(xml);
        var source = CreateSource();

        var result = await fetcher.FetchAsync(source, CancellationToken.None);

        var article = Assert.Single(result);
        Assert.Null(article.PublishedAt);
    }

    [Fact]
    public async Task FetchAsync_ShouldThrowInvalidOperationException_WhenHttpRequestFails()
    {
        var fetcher = CreateFetcher(
            request => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("server error")
            });

        var source = CreateSource(name: "Broken RSS", url: "https://example.com/broken.xml");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fetcher.FetchAsync(source, CancellationToken.None));

        Assert.Contains("Failed to fetch RSS source", exception.Message);
        Assert.Contains("Broken RSS", exception.Message);
        Assert.Contains("https://example.com/broken.xml", exception.Message);
    }

    [Fact]
    public async Task FetchAsync_ShouldThrowInvalidOperationException_WhenXmlIsMalformed()
    {
        var fetcher = CreateFetcher("<rss><channel><item></rss>");
        var source = CreateSource(name: "Malformed RSS", url: "https://example.com/malformed.xml");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fetcher.FetchAsync(source, CancellationToken.None));

        Assert.Contains("Failed to parse RSS source", exception.Message);
        Assert.Contains("Malformed RSS", exception.Message);
        Assert.Contains("https://example.com/malformed.xml", exception.Message);
    }

    [Fact]
    public async Task FetchAsync_ShouldUseSourceUrlInHttpRequest()
    {
        var requestedUris = new List<Uri?>();

        var fetcher = CreateFetcher(request =>
        {
            requestedUris.Add(request.RequestUri);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<rss><channel /></rss>")
            };
        });

        var source = CreateSource(url: "https://example.com/custom-feed.xml");

        await fetcher.FetchAsync(source, CancellationToken.None);

        var requestedUri = Assert.Single(requestedUris);
        Assert.Equal("https://example.com/custom-feed.xml", requestedUri?.ToString());
    }

    [Fact]
    public async Task FetchAsync_ShouldPropagateCancellationTokenToHttpClient()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var fetcher = CreateFetcher(_ =>
        {
            throw new InvalidOperationException("Handler should not be called after cancellation.");
        });

        var source = CreateSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fetcher.FetchAsync(source, cancellationTokenSource.Token));
    }

    private static RssNewsFetcher CreateFetcher(string responseXml)
    {
        return CreateFetcher(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseXml)
        });
    }

    private static RssNewsFetcher CreateFetcher(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(handler));

        return new RssNewsFetcher(httpClient);
    }

    private static NewsSource CreateSource(
        SourceType sourceType = SourceType.Rss,
        string name = "Test RSS",
        string url = "https://example.com/feed.xml",
        string language = "en")
    {
        var now = DateTimeOffset.UtcNow;

        return new NewsSource
        {
            Id = Guid.NewGuid(),
            Name = name,
            SourceType = sourceType,
            Url = url,
            Language = language,
            DefaultCategories = ["general"],
            IsEnabled = true,
            IsFastSource = false,
            FetchIntervalMinutes = 60,
            TrustScore = 70,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(_handler(request));
        }
    }
}