namespace NewsSummarizer.Telegram.Commands;

public static class BotCommandResponseText
{
    public static string Welcome()
    {
        return """
News Summarizer is ready.

Use /help to see available commands.
""";
    }

    public static string StatusPlaceholder()
    {
        return """
Status command is available, but runtime status is not connected yet.
""";
    }

    public static string DigestPlaceholder()
    {
        return """
Digest command is available, but digest loading is not connected yet.
""";
    }

    public static string OpportunitiesPlaceholder()
    {
        return """
Opportunities command is available, but opportunity digest loading is not connected yet.
""";
    }

    public static string AnalyzeUsage()
    {
        return "Usage: /analyze <articleId>";
    }

    public static string AnalyzePlaceholder(string articleId)
    {
        return $"Detailed analysis command is available for article {articleId}, but AI analysis execution is not connected yet.";
    }

    public static string UnknownCommand()
    {
        return "Unknown command. Use /help to see available commands.";
    }
}