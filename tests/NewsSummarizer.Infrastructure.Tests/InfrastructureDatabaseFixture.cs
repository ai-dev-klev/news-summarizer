using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace NewsSummarizer.Infrastructure.Tests;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class InfrastructureDatabaseCollection : ICollectionFixture<InfrastructureDatabaseFixture>
{
    public const string CollectionName = "Infrastructure database";
}

public sealed class InfrastructureDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("news_summarizer_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public NewsSummarizerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NewsSummarizerDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new NewsSummarizerDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var context = CreateContext();

        await context.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                article_embeddings,
                article_ai_results,
                detailed_analyses,
                digest_items,
                notifications,
                user_preferences,
                digests,
                news_articles,
                users,
                news_sources
            RESTART IDENTITY CASCADE;
            """);
    }
}
