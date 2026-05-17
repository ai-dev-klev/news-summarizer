using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Worker.Options;

namespace NewsSummarizer.Worker.Jobs;

public sealed class BuildOpportunityDigestsJob : IWorkerJob
{
    private readonly BuildOpportunityDigestUseCase _useCase;
    private readonly ILogger<BuildOpportunityDigestsJob> _logger;

    public BuildOpportunityDigestsJob(
        BuildOpportunityDigestUseCase useCase,
        ILogger<BuildOpportunityDigestsJob> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    public string Name => WorkerJobNames.BuildOpportunityDigests;

    public async Task ExecuteAsync(
        WorkerPipelineOptions options,
        CancellationToken cancellationToken)
    {
        var summary = await _useCase.ExecuteAsync(cancellationToken);

        _logger.LogInformation(
            "Build opportunity digests completed. UsersChecked: {UsersChecked}, DigestsCreated: {DigestsCreated}, UsersSkippedDisabled: {UsersSkippedDisabled}, UsersSkippedExistingDigest: {UsersSkippedExistingDigest}, UsersSkippedNoItems: {UsersSkippedNoItems}",
            summary.UsersChecked,
            summary.DigestsCreated,
            summary.UsersSkippedDisabled,
            summary.UsersSkippedExistingDigest,
            summary.UsersSkippedNoItems);
    }
}
