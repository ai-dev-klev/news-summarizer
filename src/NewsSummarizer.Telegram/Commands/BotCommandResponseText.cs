using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Telegram.Commands;

public static class BotCommandResponseText
{
    public static string Welcome(User user)
    {
        var name = string.IsNullOrWhiteSpace(user.FirstName)
            ? "пользователь"
            : user.FirstName.Trim();

        return $"""
                Привет, {name}.

                Я news-summarizer bot. Я показываю краткие новостные сводки, возможности для анализа и срочные уведомления.

                На экране должна появиться клавиатура с кнопками:
                /digest
                /opportunities
                /settings
                /help

                Доступные команды:
                /digest — ежедневная сводка
                /opportunities — сводка возможностей
                /settings — настройки
                /analyze <articleId> — подробный анализ новости
                /help — помощь
                """;
    }

    public static string Status(User user)
    {
        return $"""
                Статус профиля: {user.Status}
                TelegramId: {user.TelegramUserId}
                Username: {(string.IsNullOrWhiteSpace(user.Username) ? "[не указан]" : user.Username)}
                UserId: {user.Id}
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

    public static string UnknownCommand()
    {
        return """
               Неизвестная команда.

               Используй /help, чтобы посмотреть список доступных команд.
               """;
    }
}
