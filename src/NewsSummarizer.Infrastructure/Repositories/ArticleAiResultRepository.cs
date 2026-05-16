using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Infrastructure.Persistence;

namespace NewsSummarizer.Infrastructure.Repositories;

public sealed class ArticleAiResultRepository : IArticleAiResultRepository
{
    private readonly NewsSummarizerDbContext _dbContext;

    public ArticleAiResultRepository(NewsSummarizerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ArticleAiResult?> GetLatestSuccessfulByArticleIdAsync(Guid articleId, CancellationToken cancellationToken)
    {
        return _dbContext.ArticleAiResults
            .Where(x => x.ArticleId == articleId && x.Status == AiResultStatus.Success)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ArticleAiResult>> GetSuccessfulByArticleIdsAsync(
        IReadOnlyCollection<Guid> articleIds,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ArticleAiResults
            .Where(x => articleIds.Contains(x.ArticleId))
            .Where(x => x.Status == AiResultStatus.Success)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ArticleAiResult result, CancellationToken cancellationToken)
    {
        await _dbContext.ArticleAiResults.AddAsync(result, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}