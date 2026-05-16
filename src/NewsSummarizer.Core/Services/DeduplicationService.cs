using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.Services;

public sealed class DeduplicationService
{
    public ArticleDeduplicationKey BuildKey(NewsArticle article)
    {
        return new ArticleDeduplicationKey(
            article.Url,
            article.CanonicalUrl,
            article.NormalizedTitle,
            article.ContentHash,
            article.DedupKey);
    }
}