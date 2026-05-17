namespace NewsSummarizer.Telegram.Commands;

public sealed class BotCommandRouter
{
    public string Route(BotCommand command)
    {
        return command.Type switch
        {
            BotCommandType.Help => BotCommandHelpText.Build(),
            BotCommandType.Analyze => RouteAnalyze(command),
            BotCommandType.Start => "Р С™Р С•Р СР В°Р Р…Р Т‘Р В° /start Р С•Р В±РЎР‚Р В°Р В±Р В°РЎвЂљРЎвЂ№Р Р†Р В°Р ВµРЎвЂљРЎРѓРЎРЏ TelegramCommandService.",
            BotCommandType.Status => "Р С™Р С•Р СР В°Р Р…Р Т‘Р В° /status Р С•Р В±РЎР‚Р В°Р В±Р В°РЎвЂљРЎвЂ№Р Р†Р В°Р ВµРЎвЂљРЎРѓРЎРЏ TelegramCommandService.",
            BotCommandType.Digest => "Р С™Р С•Р СР В°Р Р…Р Т‘Р В° /digest Р С•Р В±РЎР‚Р В°Р В±Р В°РЎвЂљРЎвЂ№Р Р†Р В°Р ВµРЎвЂљРЎРѓРЎРЏ TelegramCommandService.",
            BotCommandType.Opportunities => "Р С™Р С•Р СР В°Р Р…Р Т‘Р В° /opportunities Р С•Р В±РЎР‚Р В°Р В±Р В°РЎвЂљРЎвЂ№Р Р†Р В°Р ВµРЎвЂљРЎРѓРЎРЏ TelegramCommandService.",
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

        return "Р С™Р С•Р СР В°Р Р…Р Т‘Р В° /analyze Р С•Р В±РЎР‚Р В°Р В±Р В°РЎвЂљРЎвЂ№Р Р†Р В°Р ВµРЎвЂљРЎРѓРЎРЏ TelegramCommandService.";
    }
}