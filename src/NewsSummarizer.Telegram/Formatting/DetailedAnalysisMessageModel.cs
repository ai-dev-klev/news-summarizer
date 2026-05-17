namespace NewsSummarizer.Telegram.Formatting;

public sealed record DetailedAnalysisMessageModel(
    string ArticleTitle,
    string AnalysisText,
    string? Url,
    DateTimeOffset? CreatedAt);