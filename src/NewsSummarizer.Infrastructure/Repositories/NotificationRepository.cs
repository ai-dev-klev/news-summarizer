using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Infrastructure.Persistence;

namespace NewsSummarizer.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NewsSummarizerDbContext _dbContext;

    public NotificationRepository(NewsSummarizerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(
        Guid userId,
        NotificationType type,
        string dedupKey,
        CancellationToken cancellationToken)
    {
        return _dbContext.Notifications.AnyAsync(notification =>
            notification.UserId == userId &&
            notification.NotificationType == type &&
            notification.DedupKey == dedupKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Notifications
            .Where(notification => notification.Status == NotificationStatus.Pending)
            .OrderBy(notification => notification.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}