using System.Text;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Telegram.Formatting;

public sealed class DigestMessageFormatter
{
    public string Format(LatestDigestResult? result, DigestType digestType)
    {
        var title = digestType == DigestType.Opportunity
            ? "Сводка возможностей"
            : "Ежедневная сводка";

        if (result is null)
        {
            return "Сначала отправь /start, чтобы бот создал твой профиль.";
        }

        if (result.Digest is null || result.Items.Count == 0)
        {
            return $"{title} пока не сформирована.\n\nЗапусти pipeline или проверь, что есть проанализированные новости.";
        }

        var builder = new StringBuilder();

        builder.AppendLine(title);
        builder.AppendLine($"Период: {result.Digest.PeriodStart:yyyy-MM-dd HH:mm} — {result.Digest.PeriodEnd:yyyy-MM-dd HH:mm} UTC");

        foreach (var item in result.Items.OrderBy(item => item.Position))
        {
            builder.AppendLine();
            builder.AppendLine($"{item.Position}. {item.TitleSnapshot}");

            var summary = TelegramText.Optional(item.SummarySnapshot);
            if (summary is not null)
            {
                builder.AppendLine($"Кратко: {summary}");
            }

            var reason = TelegramText.Optional(item.ReasonSnapshot);
            if (reason is not null)
            {
                builder.AppendLine(digestType == DigestType.Opportunity
                    ? $"Возможность: {reason}"
                    : $"Почему важно: {reason}");
            }

            var url = TelegramText.Optional(item.UrlSnapshot);
            if (url is not null)
            {
                builder.AppendLine($"Ссылка: {url}");
            }

            if (item.ArticleId is not null)
            {
                builder.AppendLine($"Подробнее: /analyze {item.ArticleId}");
            }
        }

        return TelegramText.Truncate(builder.ToString().Trim(), TelegramMessageLimits.SafeMessageLength);
    }
}
