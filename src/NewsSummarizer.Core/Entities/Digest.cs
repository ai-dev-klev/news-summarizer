using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Entities;

public sealed class Digest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DigestType DigestType { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public DigestStatus Status { get; set; } = DigestStatus.Created;
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}