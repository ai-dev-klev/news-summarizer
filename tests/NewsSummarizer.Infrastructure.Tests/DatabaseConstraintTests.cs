using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Infrastructure.Tests;

[Collection(InfrastructureDatabaseCollection.CollectionName)]
public sealed class DatabaseConstraintTests
{
    private readonly InfrastructureDatabaseFixture _fixture;

    public DatabaseConstraintTests(InfrastructureDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NewsSources_ShouldEnforceUniqueUrl()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        const string url = "https://example.com/rss.xml";

        await context.NewsSources.AddRangeAsync(
            InfrastructureTestData.Source(url),
            InfrastructureTestData.Source(url));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Users_ShouldEnforceUniqueTelegramUserId()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        await context.Users.AddRangeAsync(
            InfrastructureTestData.User(telegramUserId: 123),
            InfrastructureTestData.User(telegramUserId: 123));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UserPreferences_ShouldEnforceOnePreferencesRowPerUser()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var user = InfrastructureTestData.User();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        await context.UserPreferences.AddRangeAsync(
            InfrastructureTestData.Preferences(user.Id),
            InfrastructureTestData.Preferences(user.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task NewsArticles_ShouldEnforceUniqueUrl()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var source = InfrastructureTestData.Source();
        await context.NewsSources.AddAsync(source);
        await context.SaveChangesAsync();

        const string url = "https://example.com/duplicated";

        await context.NewsArticles.AddRangeAsync(
            InfrastructureTestData.Article(source.Id, url: url),
            InfrastructureTestData.Article(source.Id, url: url));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task ArticleAiResults_ShouldEnforceUniqueArticleProviderPromptVersion()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var source = InfrastructureTestData.Source();
        await context.NewsSources.AddAsync(source);

        var article = InfrastructureTestData.Article(source.Id);
        await context.NewsArticles.AddAsync(article);
        await context.SaveChangesAsync();

        await context.ArticleAiResults.AddRangeAsync(
            InfrastructureTestData.AiResult(article.Id, provider: AiProviderType.Mock, promptVersion: "v1"),
            InfrastructureTestData.AiResult(article.Id, provider: AiProviderType.Mock, promptVersion: "v1"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Theory]
    [InlineData(101, 50, 50)]
    [InlineData(50, -1, 50)]
    [InlineData(50, 50, 150)]
    public async Task ArticleAiResults_ShouldEnforceScoreRanges(
        int importanceScore,
        int urgencyScore,
        int opportunityScore)
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var source = InfrastructureTestData.Source();
        await context.NewsSources.AddAsync(source);

        var article = InfrastructureTestData.Article(source.Id);
        await context.NewsArticles.AddAsync(article);
        await context.SaveChangesAsync();

        await context.ArticleAiResults.AddAsync(InfrastructureTestData.AiResult(
            article.Id,
            importanceScore: importanceScore,
            urgencyScore: urgencyScore,
            opportunityScore: opportunityScore));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Digests_ShouldEnforceUniqueDigestPeriodPerUserAndType()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var user = InfrastructureTestData.User();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var first = InfrastructureTestData.Digest(user.Id, DigestType.Daily);
        var second = InfrastructureTestData.Digest(user.Id, DigestType.Daily);
        second.PeriodStart = first.PeriodStart;
        second.PeriodEnd = first.PeriodEnd;

        await context.Digests.AddRangeAsync(first, second);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task DigestItems_ShouldEnforceUniquePositionInsideDigest()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var user = InfrastructureTestData.User();
        var source = InfrastructureTestData.Source();
        await context.Users.AddAsync(user);
        await context.NewsSources.AddAsync(source);

        var article = InfrastructureTestData.Article(source.Id);
        await context.NewsArticles.AddAsync(article);

        var digest = InfrastructureTestData.Digest(user.Id);
        await context.Digests.AddAsync(digest);
        await context.SaveChangesAsync();

        await context.DigestItems.AddRangeAsync(
            InfrastructureTestData.DigestItem(digest.Id, article.Id, position: 1),
            InfrastructureTestData.DigestItem(digest.Id, article.Id, position: 1));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Notifications_ShouldEnforceUniqueUserTypeDedupKey()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var user = InfrastructureTestData.User();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        await context.Notifications.AddRangeAsync(
            InfrastructureTestData.Notification(user.Id, notificationType: NotificationType.Urgent, dedupKey: "same-key"),
            InfrastructureTestData.Notification(user.Id, notificationType: NotificationType.Urgent, dedupKey: "same-key"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}