using System.Text;

namespace NewsSummarizer.Telegram.Formatting;

public sealed class DigestMessageFormatter
{
    public string FormatDailyDigest(DigestMessageModel digest)
    {
        return FormatDigest("Daily digest", digest);
    }

    public string FormatOpportunityDigest(DigestMessageModel digest)
    {
        return FormatDigest("Opportunity digest", digest);
    }

    private static string FormatDigest(string heading, DigestMessageModel digest)
    {
        var builder = new StringBuilder();

        builder.AppendLine(heading);
        builder.AppendLine(TelegramText.Required(digest.Title, heading));
        builder.AppendLine($"Period: {TelegramText.FormatDate(digest.PeriodStart)} - {TelegramText.FormatDate(digest.PeriodEnd)}");

        if (digest.Items.Count == 0)
        {
            builder.AppendLine();
            builder.AppendLine("No items.");
            return TelegramText.Truncate(builder.ToString().Trim(), TelegramMessageLimits.SafeMessageLength);
        }

        foreach (var item in digest.Items.OrderBy(item => item.Position))
        {
            AppendItem(builder, item);
        }

        return TelegramText.Truncate(builder.ToString().Trim(), TelegramMessageLimits.SafeMessageLength);
    }

    private static void AppendItem(StringBuilder builder, DigestMessageItemModel item)
    {
        builder.AppendLine();
        builder.AppendLine($"{item.Position}. {TelegramText.Required(item.Title, "Untitled news")}");

        var sourceName = TelegramText.Optional(item.SourceName);
        if (sourceName is not null)
        {
            builder.AppendLine($"Source: {sourceName}");
        }

        var summary = TelegramText.Optional(item.Summary);
        if (summary is not null)
        {
            builder.AppendLine($"Summary: {summary}");
        }

        var reason = TelegramText.Optional(item.Reason);
        if (reason is not null)
        {
            builder.AppendLine($"Why it matters: {reason}");
        }

        var url = TelegramText.Optional(item.Url);
        if (url is not null)
        {
            builder.AppendLine($"Link: {url}");
        }
    }
}