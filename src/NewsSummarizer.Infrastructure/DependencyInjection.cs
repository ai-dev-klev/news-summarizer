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
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=news_summarizer;Username=postgres;Password=postgres";

        services.AddDbContext<NewsSummarizerDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<DatabaseSeeder>();

        var fetcherProvider = configuration["NewsFetching:Provider"] ?? "Mock";
        if (fetcherProvider.Equals("Rss", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<RssNewsFetcher>();
            services.AddScoped<INewsFetcher, RssNewsFetcher>();
        }
        else
        {
            services.AddScoped<INewsFetcher, MockNewsFetcher>();
        }

        return services;
    }
} 