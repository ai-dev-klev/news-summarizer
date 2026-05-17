using Microsoft.EntityFrameworkCore;

namespace NewsSummarizer.Infrastructure.Tests;

[Collection(InfrastructureDatabaseCollection.CollectionName)]
public sealed class SchemaAndMigrationTests
{
    private readonly InfrastructureDatabaseFixture _fixture;

    public SchemaAndMigrationTests(InfrastructureDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrations_ShouldBeApplied()
    {
        await using var context = _fixture.CreateContext();

        var migrations = await context.Database.GetAppliedMigrationsAsync();

        Assert.NotEmpty(migrations);
        Assert.Contains(migrations, migration => migration.Contains("InitialCreate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Database_ShouldContainExpectedTables()
    {
        await using var context = _fixture.CreateContext();

        var tables = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT table_name AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'public'
                ORDER BY table_name;
                """)
            .ToListAsync();

        Assert.Contains("users", tables);
        Assert.Contains("user_preferences", tables);
        Assert.Contains("news_sources", tables);
        Assert.Contains("news_articles", tables);
        Assert.Contains("article_ai_results", tables);
        Assert.Contains("digests", tables);
        Assert.Contains("digest_items", tables);
        Assert.Contains("notifications", tables);
        Assert.Contains("detailed_analyses", tables);
    }

    [Fact]
    public async Task Database_ShouldUseSnakeCaseColumnNames()
    {
        await using var context = _fixture.CreateContext();

        var columns = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'news_articles'
                ORDER BY column_name;
                """)
            .ToListAsync();

        Assert.Contains("source_id", columns);
        Assert.Contains("canonical_url", columns);
        Assert.Contains("normalized_title", columns);
        Assert.Contains("content_hash", columns);
        Assert.DoesNotContain("SourceId", columns);
        Assert.DoesNotContain("CanonicalUrl", columns);
    }
}