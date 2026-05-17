namespace NewsSummarizer.Telegram.Commands;

public static class BotCommandHelpText
{
    public static string Build()
    {
        return """
               Доступные команды:

               /start — зарегистрироваться или обновить профиль
               /help — показать список команд
               /status — показать текущий статус сервиса
               /digest — показать последний ежедневный дайджест
               /opportunities — показать последний дайджест перспективных новостей
               /analyze <articleId> — запросить подробный анализ новости

               Примеры:

               /digest
               /opportunities
               /analyze 00000000-0000-0000-0000-000000000000
               """;
    }
}