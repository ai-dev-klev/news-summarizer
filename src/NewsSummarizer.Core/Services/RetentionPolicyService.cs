namespace NewsSummarizer.Core.Services;

public sealed class RetentionPolicyService
{
    public DateTimeOffset GetArticleExpiration(DateTimeOffset fetchedAt)
    {
        return fetchedAt.AddDays(14);
    }

    public DateTimeOffset GetNotificationExpiration(DateTimeOffset createdAt)
    {
        return createdAt.AddDays(30);
    }
}