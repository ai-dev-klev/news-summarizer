using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewsSummarizer.Ai.Clients;
using NewsSummarizer.Ai.Models;
using NewsSummarizer.Ai.Parsing;
using NewsSummarizer.Ai.Providers;
using NewsSummarizer.Core.Interfaces;

namespace NewsSummarizer.Ai;

public static class DependencyInjection
{
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        // Backward compatibility: old section first, new section second.
        services.Configure<AiProviderOptions>(configuration.GetSection("Ai"));
        services.Configure<AiProviderOptions>(configuration.GetSection("AiProvider"));

        services.PostConfigure<AiProviderOptions>(options =>
        {
            // Env variables must win over defaults from AiProviderOptions.
            options.Provider = FirstNotEmpty(
                configuration["YANDEX_AI_PROVIDER"],
                configuration["AI_PROVIDER"],
                configuration["AiProvider:Provider"],
                configuration["Ai:Provider"],
                options.Provider,
                "Mock")!;

            options.ApiKey = FirstNotEmpty(
                configuration["YANDEX_AI_API_KEY"],
                configuration["YANDEX_API_KEY"],
                configuration["AiProvider:ApiKey"],
                configuration["Ai:ApiKey"],
                options.ApiKey,
                string.Empty)!;

            options.FolderId = FirstNotEmpty(
                configuration["YANDEX_AI_FOLDER_ID"],
                configuration["YANDEX_FOLDER_ID"],
                configuration["AiProvider:FolderId"],
                configuration["Ai:FolderId"],
                options.FolderId,
                string.Empty)!;

            options.BaseUrl = FirstNotEmpty(
                configuration["YANDEX_AI_BASE_URL"],
                configuration["YANDEX_BASE_URL"],
                configuration["AiProvider:BaseUrl"],
                configuration["Ai:BaseUrl"],
                options.BaseUrl,
                "https://ai.api.cloud.yandex.net/v1")!;

            options.Model = FirstNotEmpty(
                configuration["YANDEX_AI_MODEL"],
                configuration["YANDEX_MODEL"],
                configuration["AiProvider:Model"],
                configuration["Ai:Model"],
                options.Model,
                "yandexgpt/rc")!;

            options.PromptVersion = FirstNotEmpty(
                configuration["YANDEX_AI_PROMPT_VERSION"],
                configuration["YANDEX_PROMPT_VERSION"],
                configuration["AiProvider:PromptVersion"],
                configuration["Ai:PromptVersion"],
                options.PromptVersion,
                "v1")!;

            if (options.RequestTimeoutSeconds <= 0)
            {
                options.RequestTimeoutSeconds = 60;
            }

            if (options.MaxOutputTokens <= 0)
            {
                options.MaxOutputTokens = 800;
            }
        });

        services.AddSingleton<AiResponseParser>();
        services.AddSingleton<YandexChatClientFactory>();

        services.AddScoped<MockAiProvider>();
        services.AddScoped<YandexAiProvider>();

        services.AddScoped<IAiProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AiProviderOptions>>().Value;

            return options.Provider.Equals("Yandex", StringComparison.OrdinalIgnoreCase)
                ? serviceProvider.GetRequiredService<YandexAiProvider>()
                : serviceProvider.GetRequiredService<MockAiProvider>();
        });

        return services;
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}