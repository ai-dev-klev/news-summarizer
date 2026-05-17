namespace NewsSummarizer.Telegram.Commands;

public sealed class BotCommandParser
{
    public BotCommand Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new BotCommand(BotCommandType.Unknown, string.Empty, []);
        }

        var trimmed = text.Trim();
        var parts = trimmed
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return new BotCommand(BotCommandType.Unknown, trimmed, []);
        }

        var commandToken = parts[0];

        if (!commandToken.StartsWith('/'))
        {
            return new BotCommand(BotCommandType.Unknown, trimmed, parts.Skip(1).ToArray());
        }

        var commandName = commandToken[1..];

        var botNameSeparator = commandName.IndexOf('@');
        if (botNameSeparator >= 0)
        {
            commandName = commandName[..botNameSeparator];
        }

        var commandType = commandName.ToLowerInvariant() switch
        {
            "start" => BotCommandType.Start,
            "help" => BotCommandType.Help,
            "status" => BotCommandType.Status,
            "digest" => BotCommandType.Digest,
            "opportunities" => BotCommandType.Opportunities,
            "opportunity" => BotCommandType.Opportunities,
            "analyze" => BotCommandType.Analyze,
            "settings" => BotCommandType.Settings,
            "categories" => BotCommandType.Categories,
            "set_categories" => BotCommandType.Categories,
            "urgent_topics" => BotCommandType.UrgentTopics,
            "set_urgent_topics" => BotCommandType.UrgentTopics,
            "daily_on" => BotCommandType.DailyOn,
            "daily_off" => BotCommandType.DailyOff,
            "opportunities_on" => BotCommandType.OpportunitiesOn,
            "opportunities_off" => BotCommandType.OpportunitiesOff,
            "urgent_on" => BotCommandType.UrgentOn,
            "urgent_off" => BotCommandType.UrgentOff,
            "max_items" => BotCommandType.MaxItems,
            _ => BotCommandType.Unknown
        };

        return new BotCommand(commandType, trimmed, parts.Skip(1).ToArray());
    }
}
