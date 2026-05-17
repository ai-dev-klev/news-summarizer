using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Infrastructure.Repositories;

namespace NewsSummarizer.Infrastructure.Tests;

[Collection(InfrastructureDatabaseCollection.CollectionName)]
public sealed class CleanupRepositoryIntegrationTests
{
    private readonly InfrastructureDatabaseFixture _fixture;

    public CleanupRepositoryIntegrationTests(InfrastructureDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeleteExpiredAsync_ShouldDeleteExpiredRows_AndKeepNonExpiredRows()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var now = new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

        var user = InfrastructureTestData.User();
        var source = InfrastructureTestData.Source();

        await context.Users.AddAsync(user);
        await context.NewsSources.AddAsync(source);

        var expiredArticle = InfrastructureTestData.Article(
            source.Id,
            expiresAt: now.AddMinutes(-1));

        var activeArticle = InfrastructureTestData.Article(
            source.Id,
            expiresAt: now.AddDays(1));

        await context.NewsArticles.AddRangeAsync(expiredArticle, activeArticle);
        await context.SaveChangesAsync();

        await context.Notifications.AddRangeAsync(
            InfrastructureTestData.Notification(user.Id, articleId: expiredArticle.Id, expiresAt: now.AddMinutes(-1)),
            InfrastructureTestData.Notification(user.Id, articleId: activeArticle.Id, expiresAt: now.AddDays(1)));

        await context.DetailedAnalyses.AddRangeAsync(
            InfrastructureTestData.DetailedAnalysis(user.Id, expiredArticle.Id, expiresAt: now.AddMinutes(-1)),
            InfrastructureTestData.DetailedAnalysis(user.Id, activeArticle.Id, expiresAt: now.AddDays(1)));

        await context.SaveChangesAsync();

        var repository = new CleanupRepository(context);

        var summary = await repository.DeleteExpiredAsync(now, CancellationToken.None);

        Assert.Equal(1, summary.ExpiredArticlesDeleted);
        Assert.Equal(1, summary.ExpiredNotificationsDeleted);
        Assert.Equal(1, summary.ExpiredDetailedAnalysesDeleted);

        Assert.Equal(1, await context.NewsArticles.CountAsync());
        Assert.Equal(activeArticle.Id, (await context.NewsArticles.SingleAsync()).Id);

        Assert.Equal(1, await context.Notifications.CountAsync());
        Assert.Equal(activeArticle.Id, (await context.Notifications.SingleAsync()).ArticleId);

        Assert.Equal(1, await context.DetailedAnalyses.CountAsync());
        Assert.Equal(activeArticle.Id, (await context.DetailedAnalyses.SingleAsync()).ArticleId);
    }
}