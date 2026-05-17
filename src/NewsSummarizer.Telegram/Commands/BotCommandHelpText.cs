namespace NewsSummarizer.Telegram.Commands;

public static class BotCommandHelpText
{
    public static string Build()
    {
        return """
Available commands:

/start - register or refresh your profile
/help - show this help
/status - show current service status
/digest - show the latest daily digest
/opportunities - show the latest opportunity digest
/analyze <articleId> - request detailed analysis for an article
""";
    }
}