namespace NewsSummarizer.Telegram.Commands;

public sealed record TelegramCommandResult(
    string Text,
    object? ReplyMarkup = null,
    string? CallbackAnswerText = null);
