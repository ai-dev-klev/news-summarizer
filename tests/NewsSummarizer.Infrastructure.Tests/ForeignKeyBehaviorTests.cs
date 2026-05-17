using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Infrastructure.Tests;

[Collection(InfrastructureDatabaseCollection.CollectionName)]
public sealed class ForeignKeyBehaviorTests
{
    private readonly InfrastructureDatabaseFixture _fixture;

    public ForeignKeyBehaviorTests(InfrastructureDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeletingArticle_ShouldCascadeAiResults_AndSetNullableArticleReferencesToNull()
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

        var aiResult = InfrastructureTestData.AiResult(article.Id);
        var digestItem = InfrastructureTestData.DigestItem(digest.Id, article.Id);
        var notification = InfrastructureTestData.Notification(user.Id, articleId: article.Id);
        var analysis = InfrastructureTestData.DetailedAnalysis(user.Id, article.Id);

        await context.ArticleAiResults.AddAsync(aiResult);
        await context.DigestItems.AddAsync(digestItem);
        await context.Notifications.AddAsync(notification);
        await context.DetailedAnalyses.AddAsync(analysis);
        await context.SaveChangesAsync();

        context.NewsArticles.Remove(article);
        await context.SaveChangesAsync();

        Assert.Equal(0, await context.ArticleAiResults.CountAsync());

        var loadedDigestItem = await context.DigestItems.SingleAsync();
        var loadedNotification = await context.Notifications.SingleAsync();
        var loadedAnalysis = await context.DetailedAnalyses.SingleAsync();

        Assert.Null(loadedDigestItem.ArticleId);
        Assert.Null(loadedNotification.ArticleId);
        Assert.Null(loadedAnalysis.ArticleId);
    }

    [Fact]
    public async Task DeletingDigest_ShouldCascadeDigestItems_AndSetNotificationDigestIdToNull()
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

        await context.DigestItems.AddAsync(InfrastructureTestData.DigestItem(digest.Id, article.Id));
        await context.Notifications.AddAsync(InfrastructureTestData.Notification(
            user.Id,
            digestId: digest.Id,
            notificationType: NotificationType.DailyDigest));

        await context.SaveChangesAsync();

        context.Digests.Remove(digest);
        await context.SaveChangesAsync();

        Assert.Equal(0, await context.DigestItems.CountAsync());

        var notification = await context.Notifications.SingleAsync();
        Assert.Null(notification.DigestId);
    }

    [Fact]
    public async Task DeletingUser_ShouldCascadeOwnedRows()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var user = InfrastructureTestData.User();
        var source = InfrastructureTestData.Source();

        await context.Users.AddAsync(user);
        await context.NewsSources.AddAsync(source);

        var article = InfrastructureTestData.Article(source.Id);
        await context.NewsArticles.AddAsync(article);
        await context.SaveChangesAsync();

        var digest = InfrastructureTestData.Digest(user.Id);
        await context.UserPreferences.AddAsync(InfrastructureTestData.Preferences(user.Id));
        await context.Digests.AddAsync(digest);
        await context.DetailedAnalyses.AddAsync(InfrastructureTestData.DetailedAnalysis(user.Id, article.Id));
        await context.Notifications.AddAsync(InfrastructureTestData.Notification(user.Id, articleId: article.Id));
        await context.SaveChangesAsync();

        await context.DigestItems.AddAsync(InfrastructureTestData.DigestItem(digest.Id, article.Id));
        await context.SaveChangesAsync();

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        Assert.Equal(0, await context.Users.CountAsync());
        Assert.Equal(0, await context.UserPreferences.CountAsync());
        Assert.Equal(0, await context.Digests.CountAsync());
        Assert.Equal(0, await context.DigestItems.CountAsync());
        Assert.Equal(0, await context.Notifications.CountAsync());
        Assert.Equal(0, await context.DetailedAnalyses.CountAsync());
        Assert.Equal(1, await context.NewsSources.CountAsync());
        Assert.Equal(1, await context.NewsArticles.CountAsync());
    }
}