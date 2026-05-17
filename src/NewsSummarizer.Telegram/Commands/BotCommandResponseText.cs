namespace NewsSummarizer.Telegram.Commands;

public static class BotCommandResponseText
{
    public static string Welcome()
    {
        return """
               News Summarizer готов к работе.

               Я буду помогать получать краткие сводки новостей, срочные уведомления и подборки перспективных новостей для анализа.

               Используй /help, чтобы посмотреть доступные команды.
               """;
    }

    public static string StatusPlaceholder()
    {
        return """
               Команда /status доступна.

               Подключение реального статуса сервиса будет добавлено позже.
               """;
    }

    public static string DigestPlaceholder()
    {
        return """
               Команда /digest доступна.

               Загрузка последнего ежедневного дайджеста будет подключена позже.
               """;
    }

    public static string OpportunitiesPlaceholder()
    {
        return """
               Команда /opportunities доступна.

               Загрузка дайджеста перспективных новостей будет подключена позже.
               """;
    }

    public static string AnalyzeUsage()
    {
        return """
               Использование команды:

               /analyze <articleId>

               Пример:

               /analyze 00000000-0000-0000-0000-000000000000
               """;
    }

    public static string AnalyzePlaceholder(string articleId)
    {
        return $"""
                Команда подробного анализа доступна.

                ArticleId: {articleId}

                Запуск реального AI-анализа будет подключён позже.
                """;
    }

    public static string UnknownCommand()
    {
        return """
               Неизвестная команда.

               Используй /help, чтобы посмотреть список доступных команд.
               """;
    }
}