using System.Text;

namespace NewsSummarizer.Telegram.Formatting;

public sealed class UrgentMessageFormatter
{
    public string Format(UrgentMessageModel message)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Urgent news");
        builder.AppendLine(TelegramText.Required(message.Title, "Untitled urgent news"));

        if (message.UrgencyScore is not null)
        {
            builder.AppendLine($"Urgency score: {message.UrgencyScore}");
        }

        builder.AppendLine($"Published: {TelegramText.FormatDate(message.PublishedAt)}");

        var summary = TelegramText.Optional(message.Summary);
        if (summary is not null)
        {
            builder.AppendLine();
            builder.AppendLine($"Summary: {summary}");
        }

        var reason = TelegramText.Optional(message.Reason);
        if (reason is not null)
        {
            builder.AppendLine();
            builder.AppendLine($"Why urgent: {reason}");
        }

        var url = TelegramText.Optional(message.Url);
        if (url is not null)
        {
            builder.AppendLine();
            builder.AppendLine($"Link: {url}");
        }

        return TelegramText.Truncate(builder.ToString().Trim(), TelegramMessageLimits.SafeMessageLength);
    }
}