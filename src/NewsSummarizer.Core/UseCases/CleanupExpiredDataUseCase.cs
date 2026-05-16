using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.UseCases;

public sealed class CleanupExpiredDataUseCase
{
    private readonly ICleanupRepository _cleanupRepository;

    public CleanupExpiredDataUseCase(ICleanupRepository cleanupRepository)
    {
        _cleanupRepository = cleanupRepository;
    }

    public Task<CleanupExpiredDataSummary> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _cleanupRepository.DeleteExpiredAsync(DateTimeOffset.UtcNow, cancellationToken);
    }
}