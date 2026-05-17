namespace NewsSummarizer.Telegram.Commands;

public sealed record BotCommand(
    BotCommandType Type,
    string RawText,
    IReadOnlyList<string> Arguments)
{
    public string? FirstArgument => Arguments.Count == 0 ? null : Arguments[0];
}