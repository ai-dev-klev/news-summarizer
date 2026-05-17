using Microsoft.Extensions.Options;
using NewsSummarizer.Ai.Clients;
using NewsSummarizer.Ai.Models;

namespace NewsSummarizer.Ai.Tests;

public sealed class YandexChatClientFactoryTests
{
    [Fact]
    public void BuildModelUri_ShouldBuildUriFromFolderAndModel()
    {
        var factory = CreateFactory(new AiProviderOptions
        {
            ApiKey = "test-key",
            FolderId = "folder-123",
            BaseUrl = "https://ai.api.cloud.yandex.net/v1",
            Model = "yandexgpt/latest"
        });

        var result = factory.BuildModelUri();

        Assert.Equal("gpt://folder-123/yandexgpt/latest", result);
    }

    [Fact]
    public void BuildModelUri_ShouldReturnFullGptUriAsIs()
    {
        var factory = CreateFactory(new AiProviderOptions
        {
            ApiKey = "test-key",
            FolderId = "folder-123",
            BaseUrl = "https://ai.api.cloud.yandex.net/v1",
            Model = "gpt://folder-999/yandexgpt/latest"
        });

        var result = factory.BuildModelUri();

        Assert.Equal("gpt://folder-999/yandexgpt/latest", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildModelUri_ShouldThrow_WhenFolderIdIsMissing(string folderId)
    {
        var factory = CreateFactory(new AiProviderOptions
        {
            ApiKey = "test-key",
            FolderId = folderId,
            BaseUrl = "https://ai.api.cloud.yandex.net/v1",
            Model = "yandexgpt/latest"
        });

        var exception = Assert.Throws<InvalidOperationException>(() => factory.BuildModelUri());

        Assert.Contains("folder id", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildModelUri_ShouldThrow_WhenModelIsMissing(string model)
    {
        var factory = CreateFactory(new AiProviderOptions
        {
            ApiKey = "test-key",
            FolderId = "folder-123",
            BaseUrl = "https://ai.api.cloud.yandex.net/v1",
            Model = model
        });

        var exception = Assert.Throws<InvalidOperationException>(() => factory.BuildModelUri());

        Assert.Contains("model", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ShouldThrow_WhenApiKeyIsMissing()
    {
        var factory = CreateFactory(new AiProviderOptions
        {
            ApiKey = "",
            FolderId = "folder-123",
            BaseUrl = "https://ai.api.cloud.yandex.net/v1",
            Model = "yandexgpt/latest"
        });

        var exception = Assert.Throws<InvalidOperationException>(() => factory.Create());

        Assert.Contains("API key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ShouldThrow_WhenBaseUrlIsMissing()
    {
        var factory = CreateFactory(new AiProviderOptions
        {
            ApiKey = "test-key",
            FolderId = "folder-123",
            BaseUrl = "",
            Model = "yandexgpt/latest"
        });

        var exception = Assert.Throws<InvalidOperationException>(() => factory.Create());

        Assert.Contains("base URL", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ShouldCreateChatClient_WhenOptionsAreValid()
    {
        var factory = CreateFactory(new AiProviderOptions
        {
            ApiKey = "test-key",
            FolderId = "folder-123",
            BaseUrl = "https://ai.api.cloud.yandex.net/v1",
            Model = "yandexgpt/latest"
        });

        var client = factory.Create();

        Assert.NotNull(client);
    }

    private static YandexChatClientFactory CreateFactory(AiProviderOptions options)
    {
        return new YandexChatClientFactory(Options.Create(options));
    }
}