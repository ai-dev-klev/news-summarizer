namespace NewsSummarizer.Worker.Jobs;

public static class WorkerJobNames
{
    public const string FetchNews = "FetchNews";
    public const string AnalyzeArticles = "AnalyzeArticles";
    public const string BuildDailyDigests = "BuildDailyDigests";
    public const string BuildOpportunityDigests = "BuildOpportunityDigests";
    public const string SendUrgentNotifications = "SendUrgentNotifications";
    public const string SendPendingNotifications = "SendPendingNotifications";
    public const string CleanupExpiredData = "CleanupExpiredData";
}
