using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Core.Interfaces;

public interface IArticleAiResultRepository
{
    Task<ArticleAiResult?> GetLatestSuccessfulByArticleIdAsync(Guid articleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ArticleAiResult>> GetSuccessfulByArticleIdsAsync(IReadOnlyCollection<Guid> articleIds, CancellationToken cancellationToken);
    Task AddAsync(ArticleAiResult result, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}