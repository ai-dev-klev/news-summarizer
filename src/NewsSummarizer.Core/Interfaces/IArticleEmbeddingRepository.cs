
using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Core.Interfaces;

public interface IArticleEmbeddingRepository
{
    Task<ArticleEmbedding?> GetByArticleIdAsync(
        Guid articleId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ArticleEmbedding>> GetRecentAsync(
        DateTimeOffset since,
        int limit,
        CancellationToken cancellationToken);

    Task SaveAsync(
        ArticleEmbedding embedding,
        CancellationToken cancellationToken);
}
