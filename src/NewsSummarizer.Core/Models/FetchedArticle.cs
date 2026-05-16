namespace NewsSummarizer.Core.Models;

public sealed record FetchedArticle(
    string Title,
    string Url,
    string? Description,
    string? Content,
    string? Language,
    DateTimeOffset? PublishedAt);