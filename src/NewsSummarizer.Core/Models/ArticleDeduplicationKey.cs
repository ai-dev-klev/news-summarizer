namespace NewsSummarizer.Core.Models;

public sealed record ArticleDeduplicationKey(
    string Url,
    string? CanonicalUrl,
    string NormalizedTitle,
    string? ContentHash,
    string? DedupKey);