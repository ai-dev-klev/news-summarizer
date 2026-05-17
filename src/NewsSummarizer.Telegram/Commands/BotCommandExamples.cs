namespace NewsSummarizer.Telegram.Commands;

public static class BotCommandExamples
{
    public static IReadOnlyList<string> All =>
    [
        "/start",
        "/help",
        "/status",
        "/digest",
        "/opportunities",
        "/analyze 00000000-0000-0000-0000-000000000000"
    ];
}