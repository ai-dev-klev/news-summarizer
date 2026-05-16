using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Infrastructure.Fetching;
using NewsSummarizer.Infrastructure.Persistence;
using NewsSummarizer.Infrastructure.Repositories;

namespace NewsSummarizer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default") ??
            "Host=localhost;Port=5432;Database=news_summarizer;Username=postgres;Password=postgres";

        services.AddDbContext<NewsSummarizerDbContext>(options =>
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<DatabaseSeeder>();

        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<INewsSourceRepository, NewsSourceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddScoped<IArticleAiResultRepository, ArticleAiResultRepository>();
        services.AddScoped<IDigestRepository, DigestRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IDetailedAnalysisRepository, DetailedAnalysisRepository>();

        services.AddScoped<MockNewsFetcher>();
        services.AddHttpClient<RssNewsFetcher>();

        var provider = GetConfiguredProvider(
            configuration,
            sectionKey: "NewsFetching:Provider",
            environmentKey: "NEWS_FETCHING_PROVIDER",
            defaultValue: "Mock");

        if (string.Equals(provider, "Rss", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<INewsFetcher, RssNewsFetcher>();
        }
        else
        {
            services.AddScoped<INewsFetcher, MockNewsFetcher>();
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