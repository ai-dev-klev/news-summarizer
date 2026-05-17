using System.Text.RegularExpressions;

namespace NewsSummarizer.Telegram.Formatting;

internal static partial class TelegramText
{
    public static string Required(string? value, string fallback)
    {
        var normalized = Normalize(value);
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    public static string? Optional(string? value)
    {
        var normalized = Normalize(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        if (maxLength <= 3)
        {
            return value[..maxLength];
        }

        return value[..(maxLength - 3)].TrimEnd() + "...";
    }

    public static string FormatDate(DateTimeOffset? value)
    {
        return value?.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'") ?? "not specified";
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim();
        text = HtmlTagRegex().Replace(text, string.Empty);
        text = WhitespaceRegex().Replace(text, " ");

        return text.Trim();
    }

    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}