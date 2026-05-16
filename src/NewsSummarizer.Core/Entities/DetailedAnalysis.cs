using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Entities;

public sealed class DetailedAnalysis
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ArticleId { get; set; }
    public AiProviderType Provider { get; set; }
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = "v1";
    public string? AnalysisText { get; set; }
    public string? RawResponseJson { get; set; }
    public AiResultStatus Status { get; set; } = AiResultStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}