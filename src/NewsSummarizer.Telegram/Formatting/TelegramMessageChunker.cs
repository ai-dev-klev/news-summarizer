namespace NewsSummarizer.Telegram.Formatting;

public sealed class TelegramMessageChunker
{
    public IReadOnlyList<string> Split(
        string text,
        int maxChunkLength = TelegramMessageLimits.SafeMessageLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        if (text.Length <= maxChunkLength)
        {
            return [text];
        }

        var chunks = new List<string>();
        var remaining = text.Trim();

        while (remaining.Length > maxChunkLength)
        {
            var splitAt = remaining.LastIndexOf('\n', maxChunkLength);

            if (splitAt < maxChunkLength / 2)
            {
                splitAt = maxChunkLength;
            }

            chunks.Add(remaining[..splitAt].Trim());
            remaining = remaining[splitAt..].Trim();
        }

        if (!string.IsNullOrWhiteSpace(remaining))
        {
            chunks.Add(remaining);
        }

        return chunks;
    }
}