using System.Text;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Telegram.Formatting;

public sealed class DetailedAnalysisFormatter
{
    public string Format(DetailedAnalysis analysis)
    {
        var builder = new StringBuilder();

        if (analysis.Status == AiResultStatus.Success)
        {
            builder.AppendLine("Подробный анализ");
            builder.AppendLine();

            var text = TelegramText.Optional(analysis.AnalysisText);
            builder.AppendLine(text ?? "AI не вернул текст анализа.");
        }
        else
        {
            builder.AppendLine("Не удалось выполнить подробный анализ.");

            var error = TelegramText.Optional(analysis.ErrorMessage);
            if (error is not null)
            {
                builder.AppendLine();
                builder.AppendLine(error);
            }
        }

        builder.AppendLine();
        builder.AppendLine($"AnalysisId: {analysis.Id}");

        if (analysis.ArticleId is not null)
        {
            builder.AppendLine($"ArticleId: {analysis.ArticleId}");
        }

        return TelegramText.Truncate(builder.ToString().Trim(), TelegramMessageLimits.SafeMessageLength);
    }
}
