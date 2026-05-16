using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.Interfaces;

public interface ICleanupRepository
{
    Task<CleanupExpiredDataSummary> DeleteExpiredAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
}