using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Interfaces;

public interface IDigestRepository
{
    Task<bool> ExistsAsync(Guid userId, DigestType digestType, DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken);
    Task AddAsync(Digest digest, IReadOnlyCollection<DigestItem> items, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}