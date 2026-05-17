using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Worker.Options;

namespace NewsSummarizer.Worker.Jobs;

public sealed class FetchNewsJob : IWorkerJob
{
    private readonly FetchNewsUseCase _useCase;
    private readonly ILogger<FetchNewsJob> _logger;

    public FetchNewsJob(
        FetchNewsUseCase useCase,
        ILogger<FetchNewsJob> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    public string Name => WorkerJobNames.FetchNews;

    public async Task ExecuteAsync(
        WorkerPipelineOptions options,
        CancellationToken cancellationToken)
    {
        var summary = await _useCase.ExecuteAsync(cancellationToken);

        _logger.LogInformation(
            "Fetch news completed. SourcesChecked: {SourcesChecked}, ArticlesFetched: {ArticlesFetched}, ArticlesAdded: {ArticlesAdded}, DuplicateArticles: {DuplicateArticles}, SkippedArticles: {SkippedArticles}, FailedSources: {FailedSources}",
            summary.SourcesChecked,
            summary.ArticlesFetched,
            summary.ArticlesAdded,
            summary.DuplicateArticles,
            summary.SkippedArticles,
            summary.FailedSources);
    }
}
