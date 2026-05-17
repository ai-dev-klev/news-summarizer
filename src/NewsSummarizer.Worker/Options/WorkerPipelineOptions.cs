namespace NewsSummarizer.Worker.Options;

public sealed class WorkerPipelineOptions
{
    public bool Enabled { get; set; } = true;

    public bool RunOnStartup { get; set; } = true;

    public int LoopDelaySeconds { get; set; } = 30;

    public bool StopOnFatalError { get; set; }

    public int AnalyzeArticlesLimit { get; set; } = 20;

    public int SendPendingNotificationsLimit { get; set; } = 50;

    public Dictionary<string, WorkerJobOptions> Jobs { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class WorkerJobOptions
{
    public bool Enabled { get; set; } = true;

    public bool RunOnStartup { get; set; } = true;

    public int IntervalSeconds { get; set; } = 300;
}
