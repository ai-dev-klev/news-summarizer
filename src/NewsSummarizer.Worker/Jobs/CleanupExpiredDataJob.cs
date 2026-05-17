using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Worker.Options;

namespace NewsSummarizer.Worker.Jobs;

public sealed class CleanupExpiredDataJob : IWorkerJob
{
    private readonly CleanupExpiredDataUseCase _useCase;
    private readonly ILogger<CleanupExpiredDataJob> _logger;

    public CleanupExpiredDataJob(
        CleanupExpiredDataUseCase useCase,
        ILogger<CleanupExpiredDataJob> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    public string Name => WorkerJobNames.CleanupExpiredData;

    public async Task ExecuteAsync(
        WorkerPipelineOptions options,
        CancellationToken cancellationToken)
    {
        var summary = await _useCase.ExecuteAsync(cancellationToken);

        _logger.LogInformation(
            "Cleanup expired data completed. ExpiredArticlesDeleted: {ExpiredArticlesDeleted}, ExpiredNotificationsDeleted: {ExpiredNotificationsDeleted}, ExpiredDetailedAnalysesDeleted: {ExpiredDetailedAnalysesDeleted}",
            summary.ExpiredArticlesDeleted,
            summary.ExpiredNotificationsDeleted,
            summary.ExpiredDetailedAnalysesDeleted);
    }
}
