using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsSummarizer.Ai.Providers;
using NewsSummarizer.Core.Interfaces;

namespace NewsSummarizer.Ai;

public static class DependencyInjection
{
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        var aiProvider = configuration["Ai:Provider"] ?? "Mock";
        if (aiProvider.Equals("Yandex", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<YandexAiProvider>();
            services.AddScoped<IAiProvider, YandexAiProvider>();
        }
        else
        {
            services.AddScoped<IAiProvider, MockAiProvider>();
        }

        return services;
    }
}