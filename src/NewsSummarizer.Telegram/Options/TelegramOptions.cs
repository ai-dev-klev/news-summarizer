namespace NewsSummarizer.Telegram.Options;

public sealed class TelegramOptions
{
    public string BotToken { get; set; } = string.Empty;

    public bool PollingEnabled { get; set; } = true;

    public int PollingTimeoutSeconds { get; set; } = 25;

    public int SendChunkLength { get; set; } = 3800;
}