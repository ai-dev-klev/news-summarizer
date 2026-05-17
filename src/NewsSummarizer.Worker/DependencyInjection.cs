using Microsoft.Extensions.Options;
using NewsSummarizer.Worker.Jobs;
using NewsSummarizer.Worker.Options;

namespace NewsSummarizer.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkerPipeline(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<WorkerPipelineOptions>(
            configuration.GetSection("WorkerPipeline"));

        services.PostConfigure<WorkerPipelineOptions>(NormalizeOptions);

        services.AddScoped<IWorkerJob, FetchNewsJob>();
        services.AddScoped<IWorkerJob, AnalyzeArticlesJob>();
        services.AddScoped<IWorkerJob, BuildDailyDigestsJob>();
        services.AddScoped<IWorkerJob, BuildOpportunityDigestsJob>();
        services.AddScoped<IWorkerJob, SendUrgentNotificationsJob>();
        services.AddScoped<IWorkerJob, SendPendingNotificationsJob>();
        services.AddScoped<IWorkerJob, CleanupExpiredDataJob>();

        services.AddHostedService<WorkerService>();

        return services;
    }

    private static void NormalizeOptions(WorkerPipelineOptions options)
    {
        if (options.LoopDelaySeconds <= 0)
        {
            options.LoopDelaySeconds = 30;
        }

        if (options.AnalyzeArticlesLimit <= 0)
        {
            options.AnalyzeArticlesLimit = 20;
        }

        if (options.SendPendingNotificationsLimit <= 0)
        {
            options.SendPendingNotificationsLimit = 50;
        }

        options.Jobs ??= new Dictionary<string, WorkerJobOptions>(StringComparer.OrdinalIgnoreCase);

        EnsureJob(options, WorkerJobNames.FetchNews, intervalSeconds: 300);
        EnsureJob(options, WorkerJobNames.AnalyzeArticles, intervalSeconds: 120);
        EnsureJob(options, WorkerJobNames.BuildDailyDigests, intervalSeconds: 900);
        EnsureJob(options, WorkerJobNames.BuildOpportunityDigests, intervalSeconds: 900);
        EnsureJob(options, WorkerJobNames.SendUrgentNotifications, intervalSeconds: 180);
        EnsureJob(options, WorkerJobNames.SendPendingNotifications, intervalSeconds: 60);
        EnsureJob(options, WorkerJobNames.CleanupExpiredData, intervalSeconds: 3600);
    }

    private static void EnsureJob(
        WorkerPipelineOptions options,
        string jobName,
        int intervalSeconds)
    {
        if (!options.Jobs.TryGetValue(jobName, out var jobOptions))
        {
            options.Jobs[jobName] = new WorkerJobOptions
            {
                Enabled = true,
                RunOnStartup = true,
                IntervalSeconds = intervalSeconds
            };

            return;
        }

        if (jobOptions.IntervalSeconds <= 0)
        {
            jobOptions.IntervalSeconds = intervalSeconds;
        }
    }
}
