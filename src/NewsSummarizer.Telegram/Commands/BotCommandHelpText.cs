namespace NewsSummarizer.Telegram.Commands;

public static class BotCommandHelpText
{
    public static string Build()
    {
        return """
               Команды бота:

               Основное:
               /start — создать или обновить профиль
               /help — показать помощь
               /status — показать состояние профиля
               /digest — показать последнюю ежедневную сводку
               /opportunities — показать последнюю сводку возможностей
               /analyze <articleId> — сделать подробный AI-анализ новости

               Настройки:
               /settings — открыть настройки с кнопками
               /categories <темы> — выбрать категории текстом
               /urgent_topics <темы> — выбрать темы для срочных уведомлений текстом
               /max_items <число> — ограничить размер сводки, от 1 до 20

               Для удобства используй /settings: там категории и срочные темы можно отмечать кнопками.
               """;
    }
}
