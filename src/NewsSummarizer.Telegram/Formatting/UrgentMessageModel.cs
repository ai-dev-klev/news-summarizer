namespace NewsSummarizer.Telegram.Formatting;

public sealed record UrgentMessageModel(
    string Title,
    string? Summary,
    string? Reason,
    int? UrgencyScore,
    string? Url,
    DateTimeOffset? PublishedAt);