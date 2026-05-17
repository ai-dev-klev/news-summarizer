using System.Text;

namespace NewsSummarizer.Telegram.Formatting;

public sealed class DetailedAnalysisMessageFormatter
{
    public string Format(DetailedAnalysisMessageModel message)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Detailed analysis");
        builder.AppendLine(TelegramText.Required(message.ArticleTitle, "Untitled article"));
        builder.AppendLine($"Created: {TelegramText.FormatDate(message.CreatedAt)}");

        var url = TelegramText.Optional(message.Url);
        if (url is not null)
        {
            builder.AppendLine($"Link: {url}");
        }

        builder.AppendLine();
        builder.AppendLine(TelegramText.Required(message.AnalysisText, "Analysis is empty."));

        return TelegramText.Truncate(builder.ToString().Trim(), TelegramMessageLimits.SafeMessageLength);
    }
}