using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Worker.Options;

namespace NewsSummarizer.Worker.Jobs;

public sealed class BuildDailyDigestsJob : IWorkerJob
{
    private readonly BuildDailyDigestUseCase _useCase;
    private readonly ILogger<BuildDailyDigestsJob> _logger;

    public BuildDailyDigestsJob(
        BuildDailyDigestUseCase useCase,
        ILogger<BuildDailyDigestsJob> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    public string Name => WorkerJobNames.BuildDailyDigests;

    public async Task ExecuteAsync(
        WorkerPipelineOptions options,
        CancellationToken cancellationToken)
    {
        var summary = await _useCase.ExecuteAsync(cancellationToken);

        _logger.LogInformation(
            "Build daily digests completed. UsersChecked: {UsersChecked}, DigestsCreated: {DigestsCreated}, UsersSkippedDisabled: {UsersSkippedDisabled}, UsersSkippedExistingDigest: {UsersSkippedExistingDigest}, UsersSkippedNoItems: {UsersSkippedNoItems}",
            summary.UsersChecked,
            summary.DigestsCreated,
            summary.UsersSkippedDisabled,
            summary.UsersSkippedExistingDigest,
            summary.UsersSkippedNoItems);
    }
}
