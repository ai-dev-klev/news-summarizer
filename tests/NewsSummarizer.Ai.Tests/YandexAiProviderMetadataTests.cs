using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NewsSummarizer.Ai.Clients;
using NewsSummarizer.Ai.Models;
using NewsSummarizer.Ai.Parsing;
using NewsSummarizer.Ai.Providers;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;

namespace NewsSummarizer.Ai.Tests;

public sealed class YandexAiProviderMetadataTests
{
    [Fact]
    public void YandexAiProvider_ShouldExposeProviderMetadata()
    {
        var options = new AiProviderOptions
        {
            Provider = "Yandex",
            ApiKey = "test-key",
            FolderId = "folder-123",
            BaseUrl = "https://ai.api.cloud.yandex.net/v1",
            Model = "yandexgpt/latest",
            PromptVersion = "prompt-v42"
        };

        var provider = new YandexAiProvider(
            new YandexChatClientFactory(Options.Create(options)),
            new AiResponseParser(),
            Options.Create(options),
            NullLogger<YandexAiProvider>.Instance);

        var info = Assert.IsAssignableFrom<IAiProviderInfo>(provider);

        Assert.Equal(AiProviderType.Yandex, info.Provider);
        Assert.Equal("yandexgpt/latest", info.Model);
        Assert.Equal("prompt-v42", info.PromptVersion);
    }
}