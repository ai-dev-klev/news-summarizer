using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.Interfaces;

public interface IArticleRepository
{
    Task<NewsArticle?> FindDuplicateAsync(ArticleDeduplicationKey key, CancellationToken cancellationToken);
    Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<NewsArticle>> GetPendingAiAsync(int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<NewsArticle>> GetAnalyzedForPeriodAsync(DateTimeOffset from, DateTimeOffset to, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<NewsArticle>> GetRecentAsync(int limit, CancellationToken cancellationToken);
    Task AddAsync(NewsArticle article, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}