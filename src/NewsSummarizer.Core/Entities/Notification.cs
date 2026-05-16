using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Entities;

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ArticleId { get; set; }
    public Guid? DigestId { get; set; }
    public NotificationType NotificationType { get; set; }
    public string DedupKey { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? TitleSnapshot { get; set; }
    public string? MessageSnapshot { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}