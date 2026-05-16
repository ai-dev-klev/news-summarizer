using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Infrastructure.Persistence;

namespace NewsSummarizer.Infrastructure.Repositories;

public sealed class ArticleRepository : IArticleRepository
{
    private readonly NewsSummarizerDbContext _dbContext;

    public ArticleRepository(NewsSummarizerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<NewsArticle?> FindDuplicateAsync(ArticleDeduplicationKey key, CancellationToken cancellationToken)
    {
        return _dbContext.NewsArticles
            .Where(x => x.Status != ArticleStatus.Expired)
            .FirstOrDefaultAsync(x =>
                x.Url == key.Url ||
                (key.CanonicalUrl != null && x.CanonicalUrl == key.CanonicalUrl) ||
                x.NormalizedTitle == key.NormalizedTitle ||
                (key.ContentHash != null && x.ContentHash == key.ContentHash) ||
                (key.DedupKey != null && x.DedupKey == key.DedupKey),
                cancellationToken);
    }

    public Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.NewsArticles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<NewsArticle>> GetPendingAiAsync(int limit, CancellationToken cancellationToken)
    {
        return await _dbContext.NewsArticles
            .Where(x => x.Status == ArticleStatus.PendingAi)
            .OrderBy(x => x.FetchedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NewsArticle>> GetAnalyzedForPeriodAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.NewsArticles
            .Where(x => x.Status == ArticleStatus.Analyzed)
            .Where(x => x.PublishedAt == null || (x.PublishedAt >= from && x.PublishedAt < to))
            .OrderByDescending(x => x.PublishedAt ?? x.FetchedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NewsArticle>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        return await _dbContext.NewsArticles
            .OrderByDescending(x => x.FetchedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NewsArticle article, CancellationToken cancellationToken)
    {
        await _dbContext.NewsArticles.AddAsync(article, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}