namespace NewsSummarizer.Telegram.Commands;

public sealed class BotCommandRouter
{
    public string Route(BotCommand command)
    {
        return command.Type switch
        {
            BotCommandType.Start => BotCommandResponseText.Welcome(),
            BotCommandType.Help => BotCommandHelpText.Build(),
            BotCommandType.Status => BotCommandResponseText.StatusPlaceholder(),
            BotCommandType.Digest => BotCommandResponseText.DigestPlaceholder(),
            BotCommandType.Opportunities => BotCommandResponseText.OpportunitiesPlaceholder(),
            BotCommandType.Analyze => RouteAnalyze(command),
            _ => BotCommandResponseText.UnknownCommand()
        };
    }

    private static string RouteAnalyze(BotCommand command)
    {
        var articleId = command.FirstArgument;

        if (string.IsNullOrWhiteSpace(articleId))
        {
            return BotCommandResponseText.AnalyzeUsage();
        }

        if (!Guid.TryParse(articleId, out _))
        {
            return BotCommandResponseText.AnalyzeUsage();
        }

        return BotCommandResponseText.AnalyzePlaceholder(articleId);
    }
}