namespace NewsSummarizer.Core.Models;

public sealed record ArticleAiAnalysisResult(
    string? Category,
    int ImportanceScore,
    int UrgencyScore,
    int OpportunityScore,
    string? Summary,
    string? Reason,
    string? OpportunityReason,
    bool DailyDigestCandidate,
    bool OpportunityDigestCandidate,
    bool UrgentCandidate,
    string RawResponseJson);

public sealed record DetailedAnalysisResult(string AnalysisText, string RawResponseJson);

public sealed record NotificationMessage(string Title, string Body);

public sealed record SendNotificationResult(bool Success, string? ErrorMessage = null);