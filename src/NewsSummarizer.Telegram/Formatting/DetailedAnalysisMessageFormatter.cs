namespace NewsSummarizer.Telegram.Formatting;

public sealed class DetailedAnalysisMessageFormatter
{
    public string Format(DetailedAnalysisMessageModel message)
    {
        var title = TelegramText.Optional(message.Title) ?? "Подробный анализ";
        var body = TelegramText.Optional(message.AnalysisText) ?? "Текст анализа отсутствует.";

        return TelegramText.Truncate(
            title + "\n\n" + body,
            TelegramMessageLimits.SafeMessageLength);
    }
}
