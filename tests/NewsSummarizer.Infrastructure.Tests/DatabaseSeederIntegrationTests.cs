using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Infrastructure.Persistence;

namespace NewsSummarizer.Infrastructure.Tests;

[Collection(InfrastructureDatabaseCollection.CollectionName)]
public sealed class DatabaseSeederIntegrationTests
{
    private readonly InfrastructureDatabaseFixture _fixture;

    public DatabaseSeederIntegrationTests(InfrastructureDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateInitialSourcesUserAndPreferences_AndBeIdempotent()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var seeder = new DatabaseSeeder(context);

        await seeder.SeedAsync(CancellationToken.None);

        var sourcesAfterFirstSeed = await context.NewsSources.CountAsync();
        var usersAfterFirstSeed = await context.Users.CountAsync();
        var preferencesAfterFirstSeed = await context.UserPreferences.CountAsync();

        Assert.True(sourcesAfterFirstSeed > 0);
        Assert.True(usersAfterFirstSeed > 0);
        Assert.True(preferencesAfterFirstSeed > 0);

        await seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(sourcesAfterFirstSeed, await context.NewsSources.CountAsync());
        Assert.Equal(usersAfterFirstSeed, await context.Users.CountAsync());
        Assert.Equal(preferencesAfterFirstSeed, await context.UserPreferences.CountAsync());

        Assert.True(await context.NewsSources.AnyAsync(source => source.Url.StartsWith("mock://")));
        Assert.True(await context.Users.AnyAsync(user => user.TelegramUserId == 1));
    }
}