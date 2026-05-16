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
        return _dbContext.NewsArticles.FirstOrDefaultAsync(article =>
            article.Url == key.Url ||
            (key.CanonicalUrl != null && article.CanonicalUrl == key.CanonicalUrl) ||
            article.NormalizedTitle == key.NormalizedTitle ||
            (key.ContentHash != null && article.ContentHash == key.ContentHash) ||
            (key.DedupKey != null && article.DedupKey == key.DedupKey),
            cancellationToken);
    }

    public async Task<IReadOnlyList<NewsArticle>> GetPendingAiAsync(int limit, CancellationToken cancellationToken)
    {
        return await _dbContext.NewsArticles
            .Where(article => article.Status == ArticleStatus.PendingAi)
            .OrderBy(article => article.FetchedAt)
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