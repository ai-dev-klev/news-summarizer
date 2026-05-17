
using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Infrastructure.Persistence;

namespace NewsSummarizer.Infrastructure.Repositories;

public sealed class ArticleEmbeddingRepository : IArticleEmbeddingRepository
{
    private readonly NewsSummarizerDbContext _dbContext;

    public ArticleEmbeddingRepository(NewsSummarizerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ArticleEmbedding?> GetByArticleIdAsync(
        Guid articleId,
        CancellationToken cancellationToken)
    {
        return _dbContext.ArticleEmbeddings
            .FirstOrDefaultAsync(x => x.ArticleId == articleId, cancellationToken);
    }

    public async Task<IReadOnlyList<ArticleEmbedding>> GetRecentAsync(
        DateTimeOffset since,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ArticleEmbeddings
            .AsNoTracking()
            .Where(x => x.CreatedAt >= since)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(
        ArticleEmbedding embedding,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ArticleEmbeddings
            .FirstOrDefaultAsync(x => x.ArticleId == embedding.ArticleId, cancellationToken);

        if (existing is null)
        {
            await _dbContext.ArticleEmbeddings.AddAsync(embedding, cancellationToken);
            return;
        }

        existing.Provider = embedding.Provider;
        existing.Model = embedding.Model;
        existing.Dimensions = embedding.Dimensions;
        existing.TextHash = embedding.TextHash;
        existing.Embedding = embedding.Embedding;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
