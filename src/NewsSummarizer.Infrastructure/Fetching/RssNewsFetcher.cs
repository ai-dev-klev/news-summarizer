using System.Net.Http.Headers;
using System.Xml.Linq;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Infrastructure.Fetching;

public sealed class RssNewsFetcher : INewsFetcher
{
    private static readonly XNamespace AtomNs = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace ContentNs = "http://purl.org/rss/1.0/modules/content/";

    private readonly HttpClient _httpClient;

    public RssNewsFetcher(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("news-summarizer", "1.0"));
    }

    public async Task<IReadOnlyList<FetchedArticle>> FetchAsync(
        NewsSource source,
        CancellationToken cancellationToken)
    {
        if (source.SourceType != SourceType.Rss)
        {
            return [];
        }

        string xml;

        try
        {
            xml = await _httpClient.GetStringAsync(source.Url, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Failed to fetch RSS source '{source.Name}' from '{source.Url}'.",
                exception);
        }

        try
        {
            var doc = XDocument.Parse(xml);

            return doc.Root?.Name.LocalName == "feed"
                ? ParseAtom(doc, source.Language)
                : ParseRss(doc, source.Language);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Failed to parse RSS source '{source.Name}' from '{source.Url}'.",
                exception);
        }
    }

    private static List<FetchedArticle> ParseRss(XDocument doc, string? language)
    {
        var articles = new List<FetchedArticle>();

        foreach (var item in doc.Descendants("item"))
        {
            var title = item.Element("title")?.Value.Trim();
            var link = item.Element("link")?.Value.Trim();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
            {
                continue;
            }

            var description = item.Element("description")?.Value.Trim();
            var content = item.Element(ContentNs + "encoded")?.Value.Trim() ?? description;

            DateTimeOffset? publishedAt = null;
            var pubDate = item.Element("pubDate")?.Value.Trim();

            if (!string.IsNullOrWhiteSpace(pubDate) &&
                DateTimeOffset.TryParse(pubDate, out var parsed))
            {
                publishedAt = parsed.ToUniversalTime();
            }

            articles.Add(new FetchedArticle(title, link, description, content, language, publishedAt));
        }

        return articles;
    }

    private static List<FetchedArticle> ParseAtom(XDocument doc, string? language)
    {
        var articles = new List<FetchedArticle>();

        foreach (var entry in doc.Descendants(AtomNs + "entry"))
        {
            var title = entry.Element(AtomNs + "title")?.Value.Trim();
            var link = entry.Elements(AtomNs + "link")
                .FirstOrDefault(element => element.Attribute("rel")?.Value != "self")
                ?.Attribute("href")
                ?.Value
                .Trim();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
            {
                continue;
            }

            var summary = entry.Element(AtomNs + "summary")?.Value.Trim();
            var content = entry.Element(AtomNs + "content")?.Value.Trim() ?? summary;

            DateTimeOffset? publishedAt = null;
            var publishedText = (entry.Element(AtomNs + "published")
                                 ?? entry.Element(AtomNs + "updated"))?.Value.Trim();

            if (!string.IsNullOrWhiteSpace(publishedText) &&
                DateTimeOffset.TryParse(publishedText, out var parsed))
            {
                publishedAt = parsed.ToUniversalTime();
            }

            articles.Add(new FetchedArticle(title, link, summary, content, language, publishedAt));
        }

        return articles;
    }
}
