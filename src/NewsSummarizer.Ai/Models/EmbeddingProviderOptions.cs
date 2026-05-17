
namespace NewsSummarizer.Ai.Models;

public sealed class EmbeddingProviderOptions
{
    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "Yandex";
    public string BaseUrl { get; set; } = "https://ai.api.cloud.yandex.net/v1";
    public string Model { get; set; } = "text-search-doc/latest";
    public string ApiKey { get; set; } = string.Empty;
    public string FolderId { get; set; } = string.Empty;
    public int? Dimensions { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 60;
}
