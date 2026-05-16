using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Infrastructure.Persistence;

namespace NewsSummarizer.Infrastructure.Repositories;

public sealed class CleanupRepository : ICleanupRepository
{
    private readonly NewsSummarizerDbContext _dbContext;

    public CleanupRepository(NewsSummarizerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CleanupExpiredDataSummary> DeleteExpiredAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var expiredDetailedAnalysesDeleted = await _dbContext.DetailedAnalyses
            .Where(analysis => analysis.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        var expiredNotificationsDeleted = await _dbContext.Notifications
            .Where(notification => notification.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        var expiredArticlesDeleted = await _dbContext.NewsArticles
            .Where(article => article.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        return new CleanupExpiredDataSummary(
            expiredArticlesDeleted,
            expiredNotificationsDeleted,
            expiredDetailedAnalysesDeleted);
    }
}