namespace NewsSummarizer.Telegram.Formatting;

public static class TelegramText
{
    public static string? Optional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - 20)].TrimEnd() + "\n...[обрезано]";
    }
}
