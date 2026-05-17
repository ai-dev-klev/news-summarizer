
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Entities;

public sealed class ArticleEmbedding
{
    public Guid ArticleId { get; set; }
    public AiProviderType Provider { get; set; } = AiProviderType.Yandex;
    public string Model { get; set; } = string.Empty;
    public int Dimensions { get; set; }
    public string TextHash { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
