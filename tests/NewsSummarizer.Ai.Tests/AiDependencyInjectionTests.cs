using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewsSummarizer.Ai.Models;
using NewsSummarizer.Ai.Providers;
using NewsSummarizer.Core.Interfaces;

namespace NewsSummarizer.Ai.Tests;

public sealed class AiDependencyInjectionTests
{
    [Fact]
    public void AddAi_ShouldResolveMockProviderByDefault()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        using var provider = BuildServiceProvider(configuration);

        using var scope = provider.CreateScope();

        var aiProvider = scope.ServiceProvider.GetRequiredService<IAiProvider>();

        Assert.IsType<MockAiProvider>(aiProvider);
    }

    [Fact]
    public void AddAi_ShouldResolveYandexProvider_WhenConfigured()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AI_PROVIDER"] = "Yandex",
            ["YANDEX_AI_API_KEY"] = "test-key",
            ["YANDEX_AI_FOLDER_ID"] = "folder-123",
            ["YANDEX_AI_MODEL"] = "yandexgpt/latest",
            ["YANDEX_AI_BASE_URL"] = "https://ai.api.cloud.yandex.net/v1",
            ["YANDEX_AI_PROMPT_VERSION"] = "test-v2"
        });

        using var provider = BuildServiceProvider(configuration);

        using var scope = provider.CreateScope();

        var aiProvider = scope.ServiceProvider.GetRequiredService<IAiProvider>();

        Assert.IsType<YandexAiProvider>(aiProvider);
        var info = Assert.IsAssignableFrom<IAiProviderInfo>(aiProvider);
        Assert.Equal("yandexgpt/latest", info.Model);
        Assert.Equal("test-v2", info.PromptVersion);
    }

    [Fact]
    public void AddAi_ShouldBindOptionsFromAiProviderSection()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AiProvider:Provider"] = "Yandex",
            ["AiProvider:ApiKey"] = "section-key",
            ["AiProvider:FolderId"] = "section-folder",
            ["AiProvider:BaseUrl"] = "https://example.test/v1",
            ["AiProvider:Model"] = "yandexgpt/latest",
            ["AiProvider:PromptVersion"] = "section-v1",
            ["AiProvider:MaxOutputTokens"] = "123",
            ["AiProvider:Temperature"] = "0.7",
            ["AiProvider:RequestTimeoutSeconds"] = "45"
        });

        using var provider = BuildServiceProvider(configuration);

        var options = provider.GetRequiredService<IOptions<AiProviderOptions>>().Value;

        Assert.Equal("Yandex", options.Provider);
        Assert.Equal("section-key", options.ApiKey);
        Assert.Equal("section-folder", options.FolderId);
        Assert.Equal("https://example.test/v1", options.BaseUrl);
        Assert.Equal("yandexgpt/latest", options.Model);
        Assert.Equal("section-v1", options.PromptVersion);
        Assert.Equal(123, options.MaxOutputTokens);
        Assert.Equal(0.7f, options.Temperature);
        Assert.Equal(45, options.RequestTimeoutSeconds);
    }

    [Fact]
    public void AddAi_ShouldPreferYandexEnvironmentStyleKeysOverSectionValues()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AiProvider:Provider"] = "Mock",
            ["AiProvider:ApiKey"] = "section-key",
            ["AiProvider:FolderId"] = "section-folder",
            ["AiProvider:BaseUrl"] = "https://section.test/v1",
            ["AiProvider:Model"] = "section-model",
            ["AiProvider:PromptVersion"] = "section-v1",

            ["AI_PROVIDER"] = "Yandex",
            ["YANDEX_AI_API_KEY"] = "env-key",
            ["YANDEX_AI_FOLDER_ID"] = "env-folder",
            ["YANDEX_AI_BASE_URL"] = "https://env.test/v1",
            ["YANDEX_AI_MODEL"] = "env-model",
            ["YANDEX_AI_PROMPT_VERSION"] = "env-v1"
        });

        using var provider = BuildServiceProvider(configuration);

        var options = provider.GetRequiredService<IOptions<AiProviderOptions>>().Value;

        Assert.Equal("Yandex", options.Provider);
        Assert.Equal("env-key", options.ApiKey);
        Assert.Equal("env-folder", options.FolderId);
        Assert.Equal("https://env.test/v1", options.BaseUrl);
        Assert.Equal("env-model", options.Model);
        Assert.Equal("env-v1", options.PromptVersion);
    }

    [Fact]
    public void AddAi_ShouldNormalizeInvalidTimeoutAndTokenOptionsToDefaults()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AiProvider:RequestTimeoutSeconds"] = "0",
            ["AiProvider:MaxOutputTokens"] = "-1"
        });

        using var provider = BuildServiceProvider(configuration);

        var options = provider.GetRequiredService<IOptions<AiProviderOptions>>().Value;

        Assert.Equal(60, options.RequestTimeoutSeconds);
        Assert.Equal(800, options.MaxOutputTokens);
    }

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAi(configuration);

        return services.BuildServiceProvider(validateScopes: true);
    }
}
