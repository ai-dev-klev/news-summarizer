namespace NewsSummarizer.Telegram.Commands;

public sealed class BotCommandParser
{
    public BotCommand Parse(string? text)
    {
        var rawText = text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new BotCommand(BotCommandType.Unknown, string.Empty, []);
        }

        var parts = rawText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return new BotCommand(BotCommandType.Unknown, rawText, []);
        }

        var command = NormalizeCommandName(parts[0]);
        var arguments = parts.Skip(1).ToArray();

        var type = command switch
        {
            "/start" => BotCommandType.Start,
            "/help" => BotCommandType.Help,
            "/status" => BotCommandType.Status,
            "/digest" => BotCommandType.Digest,
            "/opportunities" => BotCommandType.Opportunities,
            "/analyze" => BotCommandType.Analyze,
            _ => BotCommandType.Unknown
        };

        return new BotCommand(type, rawText, arguments);
    }

    private static string NormalizeCommandName(string value)
    {
        var command = value.Trim().ToLowerInvariant();

        var botMentionIndex = command.IndexOf('@');
        if (botMentionIndex >= 0)
        {
            command = command[..botMentionIndex];
        }

        return command;
    }
}