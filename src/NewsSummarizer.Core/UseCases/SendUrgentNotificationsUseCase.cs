using System.Text;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.Services;

namespace NewsSummarizer.Core.UseCases;

public sealed class SendUrgentNotificationsUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IArticleAiResultRepository _articleAiResultRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly RetentionPolicyService _retentionPolicyService;

    public SendUrgentNotificationsUseCase(
        IUserRepository userRepository,
        IUserPreferencesRepository preferencesRepository,
        IArticleRepository articleRepository,
        IArticleAiResultRepository articleAiResultRepository,
        INotificationRepository notificationRepository,
        RetentionPolicyService retentionPolicyService)
    {
        _userRepository = userRepository;
        _preferencesRepository = preferencesRepository;
        _articleRepository = articleRepository;
        _articleAiResultRepository = articleAiResultRepository;
        _notificationRepository = notificationRepository;
        _retentionPolicyService = retentionPolicyService;
    }

    public async Task<SendUrgentNotificationsSummary> ExecuteAsync(CancellationToken cancellationToken)
    {
        var periodEnd = DateTimeOffset.UtcNow;
        var periodStart = periodEnd.AddHours(-24);

        return await ExecuteAsync(periodStart, periodEnd, cancellationToken);
    }

    public async Task<SendUrgentNotificationsSummary> ExecuteAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetActiveAsync(cancellationToken);

        var articles = await _articleRepository.GetAnalyzedForPeriodAsync(
            periodStart,
            periodEnd,
            limit: 200,
            cancellationToken);

        var articleIds = articles.Select(article => article.Id).ToArray();

        var aiResults = await _articleAiResultRepository.GetSuccessfulByArticleIdsAsync(
            articleIds,
            cancellationToken);

        var latestUrgentResultByArticleId = aiResults
            .Where(result => result.UrgentCandidate)
            .GroupBy(result => result.ArticleId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(result => result.CreatedAt).First());

        var urgentArticles = articles
            .Where(article => latestUrgentResultByArticleId.ContainsKey(article.Id))
            .ToList();

        var usersChecked = 0;
        var notificationsCreated = 0;
        var usersSkippedDisabled = 0;
        var notificationsSkippedExisting = 0;
        var articlesSkippedByPreferences = 0;

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            usersChecked++;

            var preferences = await _preferencesRepository.GetByUserIdAsync(user.Id, cancellationToken);

            if (preferences is null || !preferences.UrgentNotificationsEnabled)
            {
                usersSkippedDisabled++;
                continue;
            }

            var runDedupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var article in urgentArticles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var aiResult = latestUrgentResultByArticleId[article.Id];

                if (!MatchesUrgentPreferences(article, aiResult, preferences))
                {
                    articlesSkippedByPreferences++;
                    continue;
                }

                var notificationDedupKey = BuildNotificationDedupKey(user.Id, article, aiResult);

                if (!runDedupKeys.Add(notificationDedupKey))
                {
                    notificationsSkippedExisting++;
                    continue;
                }

                var exists = await _notificationRepository.ExistsAsync(
                    user.Id,
                    NotificationType.Urgent,
                    notificationDedupKey,
                    cancellationToken);

                if (exists)
                {
                    notificationsSkippedExisting++;
                    continue;
                }

                var now = DateTimeOffset.UtcNow;

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ArticleId = article.Id,
                    NotificationType = NotificationType.Urgent,
                    DedupKey = notificationDedupKey,
                    Status = NotificationStatus.Pending,
                    TitleSnapshot = $"Urgent: {article.Title}",
                    MessageSnapshot = BuildUrgentMessage(article, aiResult),
                    ExpiresAt = _retentionPolicyService.GetNotificationExpiresAt(now),
                    CreatedAt = now
                };

                await _notificationRepository.AddAsync(notification, cancellationToken);
                notificationsCreated++;
            }
        }

        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return new SendUrgentNotificationsSummary(
            usersChecked,
            urgentArticles.Count,
            notificationsCreated,
            usersSkippedDisabled,
            notificationsSkippedExisting,
            articlesSkippedByPreferences);
    }

    private static bool MatchesUrgentPreferences(
        NewsArticle article,
        ArticleAiResult aiResult,
        UserPreferences preferences)
    {
        if (preferences.UrgentTopics.Count == 0)
        {
            return true;
        }

        var searchableText = BuildSearchableText(article, aiResult);

        foreach (var topic in preferences.UrgentTopics)
        {
            if (TopicMatches(topic, searchableText))
            {
                return true;
            }
        }

        return aiResult.UrgencyScore >= 90;
    }

    private static bool TopicMatches(string topic, string searchableText)
    {
        var normalizedTopic = topic.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedTopic))
        {
            return false;
        }

        if (searchableText.Contains(normalizedTopic, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var topicTokens = normalizedTopic
            .Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (topicTokens.Any(token => searchableText.Contains(token, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return normalizedTopic switch
        {
            "market_crash" => ContainsAny(searchableText, "market", "crisis", "crash", "collapse"),
            "critical_event" => ContainsAny(searchableText, "urgent", "crisis", "emergency", "critical"),
            _ => false
        };
    }

    private static string BuildSearchableText(NewsArticle article, ArticleAiResult aiResult)
    {
        return string.Join(
            " ",
            article.Title,
            article.Description,
            article.Content,
            aiResult.Category,
            aiResult.Summary,
            aiResult.Reason,
            aiResult.OpportunityReason).ToLowerInvariant();
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildNotificationDedupKey(
        Guid userId,
        NewsArticle article,
        ArticleAiResult aiResult)
    {
        var stableArticleKey =
            article.CanonicalUrl ??
            article.DedupKey ??
            article.ContentHash ??
            article.Url ??
            article.Id.ToString("N");

        var raw = $"urgent:{userId:N}:{aiResult.PromptVersion}:{stableArticleKey}";

        return raw.Length <= 512 ? raw : raw[..512];
    }

    private static string BuildUrgentMessage(NewsArticle article, ArticleAiResult aiResult)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Urgent: {article.Title}");

        if (!string.IsNullOrWhiteSpace(aiResult.Summary))
        {
            builder.AppendLine();
            builder.AppendLine($"Summary: {aiResult.Summary}");
        }

        if (!string.IsNullOrWhiteSpace(aiResult.Reason))
        {
            builder.AppendLine();
            builder.AppendLine($"Why urgent: {aiResult.Reason}");
        }

        builder.AppendLine();
        builder.AppendLine($"Urgency score: {aiResult.UrgencyScore}");
        builder.AppendLine($"Url: {article.Url}");

        return builder.ToString();
    }
}