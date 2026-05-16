using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Interfaces;

public interface IAiProviderInfo
{
    AiProviderType Provider { get; }
    string Model { get; }
    string PromptVersion { get; }
}