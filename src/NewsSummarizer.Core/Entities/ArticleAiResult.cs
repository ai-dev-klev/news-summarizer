using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Entities;

public sealed class ArticleAiResult
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public AiProviderType Provider { get; set; }
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = "v1";
    public string? Category { get; set; }
    public int ImportanceScore { get; set; }
    public int UrgencyScore { get; set; }
    public int OpportunityScore { get; set; }
    public string? Summary { get; set; }
    public string? Reason { get; set; }
    public string? OpportunityReason { get; set; }
    public bool DailyDigestCandidate { get; set; }
    public bool OpportunityDigestCandidate { get; set; }
    public bool UrgentCandidate { get; set; }
    public AiResultStatus Status { get; set; } = AiResultStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string? RawResponseJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}