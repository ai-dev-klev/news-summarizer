using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Worker.Options;

namespace NewsSummarizer.Worker.Jobs;

public sealed class SendPendingNotificationsJob : IWorkerJob
{
    private readonly SendPendingNotificationsUseCase _useCase;
    private readonly ILogger<SendPendingNotificationsJob> _logger;

    public SendPendingNotificationsJob(
        SendPendingNotificationsUseCase useCase,
        ILogger<SendPendingNotificationsJob> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    public string Name => WorkerJobNames.SendPendingNotifications;

    public async Task ExecuteAsync(
        WorkerPipelineOptions options,
        CancellationToken cancellationToken)
    {
        var limit = Math.Max(1, options.SendPendingNotificationsLimit);
        var summary = await _useCase.ExecuteAsync(limit, cancellationToken);

        _logger.LogInformation(
            "Send pending notifications completed. NotificationsTaken: {NotificationsTaken}, NotificationsSent: {NotificationsSent}, NotificationsFailed: {NotificationsFailed}, NotificationsSkippedNoUser: {NotificationsSkippedNoUser}",
            summary.NotificationsTaken,
            summary.NotificationsSent,
            summary.NotificationsFailed,
            summary.NotificationsSkippedNoUser);
    }
}
