namespace NewsSummarizer.Core.Models;

public sealed record FetchNewsSummary(
    int SourcesChecked,
    int ArticlesFetched,
    int ArticlesAdded,
    int DuplicateArticles,
    int SkippedArticles,
    int FailedSources);

public sealed record AnalyzeArticlesSummary(
    int ArticlesTaken,
    int ArticlesAnalyzed,
    int ArticlesFailed);

public sealed record BuildDailyDigestSummary(
    int UsersChecked,
    int DigestsCreated,
    int UsersSkippedDisabled,
    int UsersSkippedExistingDigest,
    int UsersSkippedNoItems);

public sealed record BuildOpportunityDigestSummary(
    int UsersChecked,
    int DigestsCreated,
    int UsersSkippedDisabled,
    int UsersSkippedExistingDigest,
    int UsersSkippedNoItems);

public sealed record SendUrgentNotificationsSummary(
    int UsersChecked,
    int ArticlesChecked,
    int NotificationsCreated,
    int UsersSkippedDisabled,
    int NotificationsSkippedExisting,
    int ArticlesSkippedByPreferences);
