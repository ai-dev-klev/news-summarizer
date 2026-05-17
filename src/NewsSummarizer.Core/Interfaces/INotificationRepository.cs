using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Interfaces;

public interface INotificationRepository
{
    Task<bool> ExistsAsync(
        Guid userId,
        NotificationType type,
        string dedupKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Notification>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Notification>>([]);
    }

    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}