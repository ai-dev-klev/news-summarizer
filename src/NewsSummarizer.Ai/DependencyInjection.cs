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
        services.Configure<AiProviderOptions>(configuration.GetSection("AiProvider"));

        services.PostConfigure<AiProviderOptions>(options =>
        {
            options.Provider = FirstNotEmpty(
                options.Provider,
                configuration["YANDEX_AI_PROVIDER"],
                configuration["AI_PROVIDER"])
                ?? options.Provider;

            options.ApiKey = FirstNotEmpty(
                options.ApiKey,
                configuration["YANDEX_AI_API_KEY"],
                configuration["YANDEX_API_KEY"])
                ?? options.ApiKey;

            options.FolderId = FirstNotEmpty(
                options.FolderId,
                configuration["YANDEX_AI_FOLDER_ID"],
                configuration["YANDEX_FOLDER_ID"])
                ?? options.FolderId;

            options.BaseUrl = FirstNotEmpty(
                options.BaseUrl,
                configuration["YANDEX_AI_BASE_URL"],
                configuration["YANDEX_BASE_URL"])
                ?? options.BaseUrl;

            options.Model = FirstNotEmpty(
                options.Model,
                configuration["YANDEX_AI_MODEL"],
                configuration["YANDEX_MODEL"])
                ?? options.Model;

            options.PromptVersion = FirstNotEmpty(
                options.PromptVersion,
                configuration["YANDEX_AI_PROMPT_VERSION"],
                configuration["YANDEX_PROMPT_VERSION"])
                ?? options.PromptVersion;
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
