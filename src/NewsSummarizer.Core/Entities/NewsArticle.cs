using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Entities;

public sealed class NewsArticle
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? CanonicalUrl { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? Language { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
    public string NormalizedTitle { get; set; } = string.Empty;
    public string? ContentHash { get; set; }
    public string? DedupKey { get; set; }
    public Guid? DuplicateOfArticleId { get; set; }
    public ArticleStatus Status { get; set; } = ArticleStatus.New;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}