namespace NewsSummarizer.Telegram.Commands;

public static class BotCommandHelpText
{
    public static string Build()
    {
        return """
               Команды бота:

               /start — создать или обновить профиль
               /help — показать помощь
               /status — показать состояние профиля
               /digest — показать последнюю ежедневную сводку
               /opportunities — показать последнюю сводку возможностей
               /analyze <articleId> — сделать подробный AI-анализ новости

               Перед демо обычно запускается pipeline:
               seed → fetch → analyze → build digests → send notifications.
               """;
    }
}
