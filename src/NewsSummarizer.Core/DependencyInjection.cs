using Microsoft.Extensions.DependencyInjection;
using NewsSummarizer.Core.Services;
using NewsSummarizer.Core.UseCases;

namespace NewsSummarizer.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddSingleton<ArticleNormalizationService>();
        services.AddSingleton<DeduplicationService>();
        services.AddSingleton<DigestSelectionService>();
        services.AddSingleton<RetentionPolicyService>();

        services.AddScoped<FetchNewsUseCase>();
        services.AddScoped<AnalyzeArticleUseCase>();
        services.AddScoped<BuildDailyDigestUseCase>();
        services.AddScoped<BuildOpportunityDigestUseCase>();
        services.AddScoped<SendUrgentNotificationsUseCase>();
        services.AddScoped<CleanupExpiredDataUseCase>();

        return services;
    }
}