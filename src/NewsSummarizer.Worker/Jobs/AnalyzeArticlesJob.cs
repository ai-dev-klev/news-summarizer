using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Worker.Options;

namespace NewsSummarizer.Worker.Jobs;

public sealed class AnalyzeArticlesJob : IWorkerJob
{
    private readonly AnalyzeArticleUseCase _useCase;
    private readonly ILogger<AnalyzeArticlesJob> _logger;

    public AnalyzeArticlesJob(
        AnalyzeArticleUseCase useCase,
        ILogger<AnalyzeArticlesJob> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    public string Name => WorkerJobNames.AnalyzeArticles;

    public async Task ExecuteAsync(
        WorkerPipelineOptions options,
        CancellationToken cancellationToken)
    {
        var limit = Math.Max(1, options.AnalyzeArticlesLimit);
        var summary = await _useCase.ExecuteAsync(limit, cancellationToken);

        _logger.LogInformation(
            "Analyze articles completed. ArticlesTaken: {ArticlesTaken}, ArticlesAnalyzed: {ArticlesAnalyzed}, ArticlesFailed: {ArticlesFailed}",
            summary.ArticlesTaken,
            summary.ArticlesAnalyzed,
            summary.ArticlesFailed);
    }
}
