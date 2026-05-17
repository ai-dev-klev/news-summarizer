namespace NewsSummarizer.Telegram.Formatting;

public sealed class TelegramMessageChunker
{
    public IReadOnlyList<string> Split(string text, int maxLength = TelegramMessageLimits.SafeMessageLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        if (maxLength <= 0 || maxLength > TelegramMessageLimits.MaxMessageLength)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }

        var normalized = text.Trim();

        if (normalized.Length <= maxLength)
        {
            return [normalized];
        }

        var chunks = new List<string>();
        var current = normalized;

        while (current.Length > maxLength)
        {
            var splitAt = FindSplitPosition(current, maxLength);

            chunks.Add(current[..splitAt].Trim());
            current = current[splitAt..].Trim();
        }

        if (current.Length > 0)
        {
            chunks.Add(current);
        }

        return chunks;
    }

    private static int FindSplitPosition(string text, int maxLength)
    {
        var searchLength = Math.Min(maxLength, text.Length);
        var newlineIndex = text.LastIndexOf('\n', searchLength - 1, searchLength);

        if (newlineIndex > maxLength / 2)
        {
            return newlineIndex;
        }

        var spaceIndex = text.LastIndexOf(' ', searchLength - 1, searchLength);

        if (spaceIndex > maxLength / 2)
        {
            return spaceIndex;
        }

        return maxLength;
    }
}