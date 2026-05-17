using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Worker.Options;

namespace NewsSummarizer.Worker.Jobs;

public sealed class SendUrgentNotificationsJob : IWorkerJob
{
    private readonly SendUrgentNotificationsUseCase _useCase;
    private readonly ILogger<SendUrgentNotificationsJob> _logger;

    public SendUrgentNotificationsJob(
        SendUrgentNotificationsUseCase useCase,
        ILogger<SendUrgentNotificationsJob> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    public string Name => WorkerJobNames.SendUrgentNotifications;

    public async Task ExecuteAsync(
        WorkerPipelineOptions options,
        CancellationToken cancellationToken)
    {
        var summary = await _useCase.ExecuteAsync(cancellationToken);

        _logger.LogInformation(
            "Create urgent notifications completed. UsersChecked: {UsersChecked}, ArticlesChecked: {ArticlesChecked}, NotificationsCreated: {NotificationsCreated}, UsersSkippedDisabled: {UsersSkippedDisabled}, NotificationsSkippedExisting: {NotificationsSkippedExisting}, ArticlesSkippedByPreferences: {ArticlesSkippedByPreferences}",
            summary.UsersChecked,
            summary.ArticlesChecked,
            summary.NotificationsCreated,
            summary.UsersSkippedDisabled,
            summary.NotificationsSkippedExisting,
            summary.ArticlesSkippedByPreferences);
    }
}
