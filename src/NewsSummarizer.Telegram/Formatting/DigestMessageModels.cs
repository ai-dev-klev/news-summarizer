namespace NewsSummarizer.Telegram.Formatting;

public sealed record DigestMessageModel(
    string Title,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd,
    IReadOnlyList<DigestMessageItemModel> Items);

public sealed record DigestMessageItemModel(
    int Position,
    string Title,
    string? Summary,
    string? Reason,
    string? SourceName,
    string? Url);