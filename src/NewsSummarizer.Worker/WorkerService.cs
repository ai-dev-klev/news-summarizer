using System.Diagnostics;
using Microsoft.Extensions.Options;
using NewsSummarizer.Worker.Jobs;
using NewsSummarizer.Worker.Options;

namespace NewsSummarizer.Worker;

public sealed class WorkerService : BackgroundService
{
    private static readonly IReadOnlyDictionary<string, int> JobOrder =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [WorkerJobNames.FetchNews] = 10,
            [WorkerJobNames.AnalyzeArticles] = 20,
            [WorkerJobNames.BuildDailyDigests] = 30,
            [WorkerJobNames.BuildOpportunityDigests] = 40,
            [WorkerJobNames.SendUrgentNotifications] = 50,
            [WorkerJobNames.SendPendingNotifications] = 60,
            [WorkerJobNames.CleanupExpiredData] = 70
        };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<WorkerPipelineOptions> _optionsMonitor;
    private readonly ILogger<WorkerService> _logger;
    private readonly Dictionary<string, DateTimeOffset> _lastRuns = new(StringComparer.OrdinalIgnoreCase);

    public WorkerService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<WorkerPipelineOptions> optionsMonitor,
        ILogger<WorkerService> logger)
    {
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker pipeline started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _optionsMonitor.CurrentValue;

            try
            {
                if (options.Enabled)
                {
                    await RunDueJobsAsync(options, stoppingToken);
                }
                else
                {
                    _logger.LogDebug("Worker pipeline is disabled.");
                }

                await DelayAsync(options, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Worker pipeline iteration failed.");

                if (options.StopOnFatalError)
                {
                    throw;
                }

                await DelayAsync(options, stoppingToken);
            }
        }

        _logger.LogInformation("Worker pipeline stopped.");
    }

    private async Task RunDueJobsAsync(
        WorkerPipelineOptions options,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var jobs = scope.ServiceProvider
            .GetServices<IWorkerJob>()
            .OrderBy(job => GetJobOrder(job.Name))
            .ToArray();

        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var jobOptions = GetJobOptions(options, job.Name);

            if (!jobOptions.Enabled)
            {
                continue;
            }

            if (!IsDue(job.Name, options, jobOptions))
            {
                continue;
            }

            await RunJobSafelyAsync(job, options, cancellationToken);
        }
    }

    private async Task RunJobSafelyAsync(
        IWorkerJob job,
        WorkerPipelineOptions options,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Worker job started. Job: {Job}", job.Name);

        try
        {
            await job.ExecuteAsync(options, cancellationToken);

            _logger.LogInformation(
                "Worker job finished. Job: {Job}, ElapsedMs: {ElapsedMs}",
                job.Name,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Worker job failed. Job: {Job}, ElapsedMs: {ElapsedMs}",
                job.Name,
                stopwatch.ElapsedMilliseconds);

            if (options.StopOnFatalError)
            {
                throw;
            }
        }
        finally
        {
            _lastRuns[job.Name] = DateTimeOffset.UtcNow;
        }
    }

    private bool IsDue(
        string jobName,
        WorkerPipelineOptions pipelineOptions,
        WorkerJobOptions jobOptions)
    {
        var now = DateTimeOffset.UtcNow;

        if (!_lastRuns.TryGetValue(jobName, out var lastRun))
        {
            if (pipelineOptions.RunOnStartup && jobOptions.RunOnStartup)
            {
                return true;
            }

            _lastRuns[jobName] = now;
            return false;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, jobOptions.IntervalSeconds));

        return now - lastRun >= interval;
    }

    private static WorkerJobOptions GetJobOptions(
        WorkerPipelineOptions options,
        string jobName)
    {
        return options.Jobs.TryGetValue(jobName, out var jobOptions)
            ? jobOptions
            : new WorkerJobOptions();
    }

    private static int GetJobOrder(string jobName)
    {
        return JobOrder.TryGetValue(jobName, out var order)
            ? order
            : int.MaxValue;
    }

    private static Task DelayAsync(
        WorkerPipelineOptions options,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(1, options.LoopDelaySeconds));
        return Task.Delay(delay, cancellationToken);
    }
}
