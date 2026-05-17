namespace NewsSummarizer.Telegram.Api;

public sealed record TelegramSendResult(
    bool Success,
    string? ErrorMessage = null);