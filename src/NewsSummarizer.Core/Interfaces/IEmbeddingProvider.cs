
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.Interfaces;

public interface IEmbeddingProvider
{
    bool IsEnabled { get; }
    AiProviderType Provider { get; }
    string Model { get; }

    Task<EmbeddingResult> CreateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default);
}
