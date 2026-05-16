namespace NewsSummarizer.Core.Services;

public sealed class RetentionPolicyService
{
    public DateTimeOffset GetArticleExpiresAt(DateTimeOffset now)
    {
        return now.AddDays(14);
    }

    public DateTimeOffset GetNotificationExpiresAt(DateTimeOffset now)
    {
        return now.AddDays(30);
    }

    public DateTimeOffset GetDetailedAnalysisExpiresAt(DateTimeOffset now)
    {
        return now.AddDays(30);
    }
}