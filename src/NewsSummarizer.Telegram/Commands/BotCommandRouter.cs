namespace NewsSummarizer.Telegram.Commands;

public sealed class BotCommandRouter
{
    public string Route(BotCommand command)
    {
        return command.Type switch
        {
            BotCommandType.Help => BotCommandHelpText.Build(),
            BotCommandType.Analyze => RouteAnalyze(command),
            BotCommandType.Start => "Команда /start обрабатывается TelegramCommandService.",
            BotCommandType.Status => "Команда /status обрабатывается TelegramCommandService.",
            BotCommandType.Digest => "Команда /digest обрабатывается TelegramCommandService.",
            BotCommandType.Opportunities => "Команда /opportunities обрабатывается TelegramCommandService.",
            BotCommandType.Settings => "Команда /settings обрабатывается TelegramCommandService.",
            BotCommandType.Categories => "Команда /categories обрабатывается TelegramCommandService.",
            BotCommandType.UrgentTopics => "Команда /urgent_topics обрабатывается TelegramCommandService.",
            BotCommandType.MaxItems => "Команда /max_items обрабатывается TelegramCommandService.",
            _ => BotCommandResponseText.UnknownCommand()
        };
    }

    private static string RouteAnalyze(BotCommand command)
    {
        var articleId = command.FirstArgument;

        if (string.IsNullOrWhiteSpace(articleId) || !Guid.TryParse(articleId, out _))
        {
            return BotCommandResponseText.AnalyzeUsage();
        }

        return "Команда /analyze обрабатывается TelegramCommandService.";
    }
}
