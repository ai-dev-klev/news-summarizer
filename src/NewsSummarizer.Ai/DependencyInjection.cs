using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewsSummarizer.Ai.Clients;
using NewsSummarizer.Ai.Models;
using NewsSummarizer.Ai.Parsing;
using NewsSummarizer.Ai.Providers;
using NewsSummarizer.Core.Interfaces;
using System.Net.Http.Headers;
using NewsSummarizer.Ai.Embeddings;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.Services;

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
services.Configure<EmbeddingProviderOptions>(configuration.GetSection("Embeddings"));
        services.Configure<EmbeddingProviderOptions>(configuration.GetSection("YandexAi:Embeddings"));
        services.Configure<EmbeddingProviderOptions>(configuration.GetSection("AiProvider:Embeddings"));

        services.PostConfigure<EmbeddingProviderOptions>(options =>
        {
            options.Enabled = FirstBool(
                configuration["EMBEDDINGS_ENABLED"],
                configuration["YANDEX_EMBEDDINGS_ENABLED"],
                configuration["Embeddings:Enabled"],
                configuration["YandexAi:Embeddings:Enabled"],
                options.Enabled);

            options.Provider = FirstNotEmpty(
                configuration["EMBEDDINGS_PROVIDER"],
                configuration["YANDEX_EMBEDDINGS_PROVIDER"],
                configuration["Embeddings:Provider"],
                configuration["YandexAi:Embeddings:Provider"],
                options.Provider,
                "Yandex")!;

            options.ApiKey = FirstNotEmpty(
                configuration["YANDEX_EMBEDDINGS_API_KEY"],
                configuration["EMBEDDINGS_API_KEY"],
                configuration["YANDEX_AI_API_KEY"],
                configuration["YANDEX_API_KEY"],
                configuration["Embeddings:ApiKey"],
                configuration["YandexAi:Embeddings:ApiKey"],
                configuration["AiProvider:ApiKey"],
                configuration["Ai:ApiKey"],
                options.ApiKey,
                string.Empty)!;

            options.FolderId = FirstNotEmpty(
                configuration["YANDEX_EMBEDDINGS_FOLDER_ID"],
                configuration["EMBEDDINGS_FOLDER_ID"],
                configuration["YANDEX_AI_FOLDER_ID"],
                configuration["YANDEX_FOLDER_ID"],
                configuration["Embeddings:FolderId"],
                configuration["YandexAi:Embeddings:FolderId"],
                configuration["AiProvider:FolderId"],
                configuration["Ai:FolderId"],
                options.FolderId,
                string.Empty)!;

            options.BaseUrl = FirstNotEmpty(
                configuration["YANDEX_EMBEDDINGS_BASE_URL"],
                configuration["EMBEDDINGS_BASE_URL"],
                configuration["YANDEX_AI_BASE_URL"],
                configuration["YANDEX_BASE_URL"],
                configuration["Embeddings:BaseUrl"],
                configuration["YandexAi:Embeddings:BaseUrl"],
                options.BaseUrl,
                "https://ai.api.cloud.yandex.net/v1")!;

            options.Model = FirstNotEmpty(
                configuration["YANDEX_EMBEDDINGS_MODEL"],
                configuration["EMBEDDINGS_MODEL"],
                configuration["Embeddings:Model"],
                configuration["YandexAi:Embeddings:Model"],
                options.Model,
                "text-search-doc/latest")!;

            options.Dimensions = FirstNullableInt(
                configuration["YANDEX_EMBEDDINGS_DIMENSIONS"],
                configuration["EMBEDDINGS_DIMENSIONS"],
                configuration["Embeddings:Dimensions"],
                configuration["YandexAi:Embeddings:Dimensions"],
                options.Dimensions);

            if (options.RequestTimeoutSeconds <= 0)
            {
                options.RequestTimeoutSeconds = 60;
            }
        });

        services.Configure<SemanticDeduplicationOptions>(configuration.GetSection("SemanticDeduplication"));
        services.PostConfigure<SemanticDeduplicationOptions>(options =>
        {
            options.Enabled = FirstBool(
                configuration["SEMANTIC_DEDUPLICATION_ENABLED"],
                configuration["SemanticDeduplication:Enabled"],
                options.Enabled);

            options.LookbackHours = FirstInt(
                configuration["SEMANTIC_DEDUPLICATION_LOOKBACK_HOURS"],
                configuration["SemanticDeduplication:LookbackHours"],
                options.LookbackHours);

            options.RecentCandidateLimit = FirstInt(
                configuration["SEMANTIC_DEDUPLICATION_RECENT_CANDIDATE_LIMIT"],
                configuration["SemanticDeduplication:RecentCandidateLimit"],
                options.RecentCandidateLimit);

            options.DuplicateThreshold = FirstDouble(
                configuration["SEMANTIC_DEDUPLICATION_DUPLICATE_THRESHOLD"],
                configuration["SemanticDeduplication:DuplicateThreshold"],
                options.DuplicateThreshold);

            options.MinTextLength = FirstInt(
                configuration["SEMANTIC_DEDUPLICATION_MIN_TEXT_LENGTH"],
                configuration["SemanticDeduplication:MinTextLength"],
                options.MinTextLength);
        });

        services.AddHttpClient<IEmbeddingProvider, YandexEmbeddingProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EmbeddingProviderOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Api-Key", options.ApiKey);
            }
        });

        services.AddScoped<ISemanticArticleDuplicateDetector>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SemanticDeduplicationOptions>>().Value;

            return new SemanticArticleDuplicateDetector(
                serviceProvider.GetRequiredService<IEmbeddingProvider>(),
                serviceProvider.GetRequiredService<IArticleEmbeddingRepository>(),
                options);
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
private static bool FirstBool(params object?[] values)
    {
        foreach (var value in values)
        {
            switch (value)
            {
                case bool booleanValue:
                    return booleanValue;
                case string text when bool.TryParse(text.Trim(), out var parsed):
                    return parsed;
            }
        }

        return false;
    }

    private static int FirstInt(string? first, string? second, int currentValue)
    {
        if (int.TryParse(first, out var firstParsed))
        {
            return firstParsed;
        }

        if (int.TryParse(second, out var secondParsed))
        {
            return secondParsed;
        }

        return currentValue;
    }

    private static int? FirstNullableInt(string? first, string? second, string? third, string? fourth, int? currentValue)
    {
        foreach (var value in new[] { first, second, third, fourth })
        {
            if (int.TryParse(value, out var parsed) && parsed > 0)
            {
                return parsed;
            }
        }

        return currentValue.HasValue && currentValue.Value > 0 ? currentValue : null;
    }

    private static double FirstDouble(string? first, string? second, double currentValue)
    {
        if (double.TryParse(first, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var firstParsed))
        {
            return firstParsed;
        }

        if (double.TryParse(second, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var secondParsed))
        {
            return secondParsed;
        }

        return currentValue;
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
