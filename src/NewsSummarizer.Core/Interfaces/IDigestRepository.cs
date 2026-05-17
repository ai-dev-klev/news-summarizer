using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Interfaces;

public interface IDigestRepository
{
    Task<bool> ExistsAsync(
        Guid userId,
        DigestType digestType,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);

    Task AddAsync(
        Digest digest,
        IReadOnlyCollection<DigestItem> items,
        CancellationToken cancellationToken);

    Task<Digest?> GetLatestByUserIdAsync(
        Guid userId,
        DigestType digestType,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Digest?>(null);
    }

    Task<IReadOnlyList<DigestItem>> GetItemsAsync(
        Guid digestId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<DigestItem>>([]);
    }

    Task SaveChangesAsync(CancellationToken cancellationToken);
}