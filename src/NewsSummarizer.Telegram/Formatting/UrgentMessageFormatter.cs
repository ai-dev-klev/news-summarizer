using System.Text;

namespace NewsSummarizer.Telegram.Formatting;

public sealed class UrgentMessageFormatter
{
    public string Format(UrgentMessageModel message)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Срочно: {message.Title}");

        if (message.UrgencyScore is not null)
        {
            builder.AppendLine($"Срочность: {message.UrgencyScore}/100");
        }

        var summary = TelegramText.Optional(message.Summary);
        if (summary is not null)
        {
            builder.AppendLine();
            builder.AppendLine($"Кратко: {summary}");
        }

        var reason = TelegramText.Optional(message.Reason);
        if (reason is not null)
        {
            builder.AppendLine();
            builder.AppendLine($"Почему срочно: {reason}");
        }

        var url = TelegramText.Optional(message.Url);
        if (url is not null)
        {
            builder.AppendLine();
            builder.AppendLine($"Ссылка: {url}");
        }

        return TelegramText.Truncate(builder.ToString().Trim(), TelegramMessageLimits.SafeMessageLength);
    }
}
