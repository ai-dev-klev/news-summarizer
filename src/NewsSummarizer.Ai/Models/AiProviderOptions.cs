namespace NewsSummarizer.Ai.Models;

public sealed class AiProviderOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = "v1";
}