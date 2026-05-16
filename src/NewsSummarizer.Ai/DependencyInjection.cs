using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsSummarizer.Ai.Providers;
using NewsSummarizer.Core.Interfaces;

namespace NewsSummarizer.Ai;

public static class DependencyInjection
{
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<MockAiProvider>();
        services.AddHttpClient<YandexAiProvider>();

        var provider = GetConfiguredProvider(
            configuration,
            sectionKey: "Ai:Provider",
            environmentKey: "AI_PROVIDER",
            defaultValue: "Mock");

        if (string.Equals(provider, "Yandex", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IAiProvider, YandexAiProvider>();
        }
        else
        {
            services.AddScoped<IAiProvider, MockAiProvider>();
        }

        return services;
    }

    private static string GetConfiguredProvider(
        IConfiguration configuration,
        string sectionKey,
        string environmentKey,
        string defaultValue)
    {
        var value =
            Environment.GetEnvironmentVariable(environmentKey) ??
            configuration[sectionKey];

        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();
    }
}