using NewsSummarizer.Worker.Options;

namespace NewsSummarizer.Worker.Jobs;

public interface IWorkerJob
{
    string Name { get; }

    Task ExecuteAsync(
        WorkerPipelineOptions options,
        CancellationToken cancellationToken);
}
