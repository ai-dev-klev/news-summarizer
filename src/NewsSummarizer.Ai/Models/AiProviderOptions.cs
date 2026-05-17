namespace NewsSummarizer.Ai.Models;

public sealed class AiProviderOptions
{
    public string Provider { get; set; } = "Mock";

    public string BaseUrl { get; set; } = "https://ai.api.cloud.yandex.net/v1";

    public string Model { get; set; } = "yandexgpt/rc";

    public string ApiKey { get; set; } = string.Empty;

    public string FolderId { get; set; } = string.Empty;

    public string PromptVersion { get; set; } = "v1";

    public int MaxOutputTokens { get; set; } = 800;

    public float Temperature { get; set; } = 0.2f;

    public int RequestTimeoutSeconds { get; set; } = 60;
}
