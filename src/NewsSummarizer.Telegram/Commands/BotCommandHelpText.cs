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
               /settings — показать текущие настройки
               /categories <темы> — выбрать категории через пробел или запятую
               /urgent_topics <темы> — выбрать темы для срочных уведомлений
               /max_items <число> — ограничить размер сводки, от 1 до 20
               /daily_on или /daily_off — включить или выключить ежедневную сводку
               /opportunities_on или /opportunities_off — включить или выключить сводку возможностей
               /urgent_on или /urgent_off — включить или выключить срочные уведомления

               Примеры:
               /categories technology business science
               /categories технологии бизнес наука
               /urgent_topics crisis security market
               /max_items 5
               """;
    }
}
