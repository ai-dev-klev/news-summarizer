using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Infrastructure.Repositories;

namespace NewsSummarizer.Infrastructure.Tests;

[Collection(InfrastructureDatabaseCollection.CollectionName)]
public sealed class RepositoryIntegrationTests
{
    private readonly InfrastructureDatabaseFixture _fixture;

    public RepositoryIntegrationTests(InfrastructureDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NewsSourceRepository_ShouldReturnEnabledAndFastSources()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var enabledFast = InfrastructureTestData.Source(enabled: true, fast: true);
        var enabledSlow = InfrastructureTestData.Source(enabled: true, fast: false);
        var disabledFast = InfrastructureTestData.Source(enabled: false, fast: true);

        await context.NewsSources.AddRangeAsync(enabledFast, enabledSlow, disabledFast);
        await context.SaveChangesAsync();

        var repository = new NewsSourceRepository(context);

        var enabledSources = await repository.GetEnabledAsync(CancellationToken.None);
        var fastSources = await repository.GetEnabledFastSourcesAsync(CancellationToken.None);

        Assert.Equal(2, enabledSources.Count);
        Assert.Contains(enabledFast.Id, enabledSources.Select(source => source.Id));
        Assert.Contains(enabledSlow.Id, enabledSources.Select(source => source.Id));
        Assert.DoesNotContain(disabledFast.Id, enabledSources.Select(source => source.Id));

        var fastSource = Assert.Single(fastSources);
        Assert.Equal(enabledFast.Id, fastSource.Id);
    }

    [Fact]
    public async Task ArticleRepository_ShouldFindDuplicatesByAllSupportedKeys_AndIgnoreExpired()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var source = InfrastructureTestData.Source();
        await context.NewsSources.AddAsync(source);

        var active = InfrastructureTestData.Article(
            source.Id,
            url: "https://example.com/original",
            canonicalUrl: "https://example.com/canonical",
            normalizedTitle: "same normalized title",
            contentHash: "same-hash",
            dedupKey: "same-dedup",
            status: ArticleStatus.Analyzed);

        var expired = InfrastructureTestData.Article(
            source.Id,
            url: "https://example.com/expired",
            canonicalUrl: "https://example.com/expired-canonical",
            normalizedTitle: "expired title",
            contentHash: "expired-hash",
            dedupKey: "expired-dedup",
            status: ArticleStatus.Expired);

        await context.NewsArticles.AddRangeAsync(active, expired);
        await context.SaveChangesAsync();

        var repository = new ArticleRepository(context);

        Assert.NotNull(await repository.FindDuplicateAsync(
            new ArticleDeduplicationKey("https://example.com/original", null, "missing", null, null),
            CancellationToken.None));

        Assert.NotNull(await repository.FindDuplicateAsync(
            new ArticleDeduplicationKey("https://example.com/missing", "https://example.com/canonical", "missing", null, null),
            CancellationToken.None));

        Assert.NotNull(await repository.FindDuplicateAsync(
            new ArticleDeduplicationKey("https://example.com/missing", null, "same normalized title", null, null),
            CancellationToken.None));

        Assert.NotNull(await repository.FindDuplicateAsync(
            new ArticleDeduplicationKey("https://example.com/missing", null, "missing", "same-hash", null),
            CancellationToken.None));

        Assert.NotNull(await repository.FindDuplicateAsync(
            new ArticleDeduplicationKey("https://example.com/missing", null, "missing", null, "same-dedup"),
            CancellationToken.None));

        Assert.Null(await repository.FindDuplicateAsync(
            new ArticleDeduplicationKey("https://example.com/expired", "https://example.com/expired-canonical", "expired title", "expired-hash", "expired-dedup"),
            CancellationToken.None));
    }

    [Fact]
    public async Task ArticleRepository_ShouldReturnPendingAiInFetchedAtOrder()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var source = InfrastructureTestData.Source();
        await context.NewsSources.AddAsync(source);

        var laterPending = InfrastructureTestData.Article(
            source.Id,
            status: ArticleStatus.PendingAi,
            fetchedAt: new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero));

        var earlierPending = InfrastructureTestData.Article(
            source.Id,
            status: ArticleStatus.PendingAi,
            fetchedAt: new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero));

        var analyzed = InfrastructureTestData.Article(
            source.Id,
            status: ArticleStatus.Analyzed,
            fetchedAt: new DateTimeOffset(2026, 5, 17, 9, 0, 0, TimeSpan.Zero));

        await context.NewsArticles.AddRangeAsync(laterPending, earlierPending, analyzed);
        await context.SaveChangesAsync();

        var repository = new ArticleRepository(context);

        var result = await repository.GetPendingAiAsync(limit: 10, CancellationToken.None);

        Assert.Collection(
            result,
            first => Assert.Equal(earlierPending.Id, first.Id),
            second => Assert.Equal(laterPending.Id, second.Id));
    }

    [Fact]
    public async Task ArticleAiResultRepository_ShouldReturnLatestSuccessfulResult()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var source = InfrastructureTestData.Source();
        await context.NewsSources.AddAsync(source);

        var article = InfrastructureTestData.Article(source.Id);
        await context.NewsArticles.AddAsync(article);

        var olderSuccess = InfrastructureTestData.AiResult(
            article.Id,
            status: AiResultStatus.Success,
            promptVersion: "older",
            createdAt: new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero));

        var latestSuccess = InfrastructureTestData.AiResult(
            article.Id,
            status: AiResultStatus.Success,
            promptVersion: "latest",
            createdAt: new DateTimeOffset(2026, 5, 17, 11, 0, 0, TimeSpan.Zero));

        var latestFailed = InfrastructureTestData.AiResult(
            article.Id,
            status: AiResultStatus.Failed,
            promptVersion: "failed",
            createdAt: new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero));

        await context.ArticleAiResults.AddRangeAsync(olderSuccess, latestSuccess, latestFailed);
        await context.SaveChangesAsync();

        var repository = new ArticleAiResultRepository(context);

        var result = await repository.GetLatestSuccessfulByArticleIdAsync(article.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(latestSuccess.Id, result.Id);
    }

    [Fact]
    public async Task DigestRepository_ShouldAddDigestWithItems_AndDetectExistingDigest()
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

        var digest = InfrastructureTestData.Digest(user.Id, DigestType.Daily);
        var items = new[]
        {
            InfrastructureTestData.DigestItem(digest.Id, article.Id, position: 1),
            InfrastructureTestData.DigestItem(digest.Id, article.Id, position: 2)
        };

        var repository = new DigestRepository(context);

        Assert.False(await repository.ExistsAsync(user.Id, digest.DigestType, digest.PeriodStart, digest.PeriodEnd, CancellationToken.None));

        await repository.AddAsync(digest, items, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        Assert.True(await repository.ExistsAsync(user.Id, digest.DigestType, digest.PeriodStart, digest.PeriodEnd, CancellationToken.None));
        Assert.Equal(2, await context.DigestItems.CountAsync());
    }

    [Fact]
    public async Task NotificationRepository_ShouldAddNotification_AndDetectByDedupKey()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var user = InfrastructureTestData.User();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new NotificationRepository(context);

        var notification = InfrastructureTestData.Notification(
            user.Id,
            notificationType: NotificationType.Urgent,
            dedupKey: "urgent:test");

        Assert.False(await repository.ExistsAsync(
            user.Id,
            NotificationType.Urgent,
            "urgent:test",
            CancellationToken.None));

        await repository.AddAsync(notification, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        Assert.True(await repository.ExistsAsync(
            user.Id,
            NotificationType.Urgent,
            "urgent:test",
            CancellationToken.None));

        Assert.False(await repository.ExistsAsync(
            user.Id,
            NotificationType.DailyDigest,
            "urgent:test",
            CancellationToken.None));
    }

    [Fact]
    public async Task DetailedAnalysisRepository_ShouldAddAndGetById()
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

        var repository = new DetailedAnalysisRepository(context);

        var analysis = InfrastructureTestData.DetailedAnalysis(user.Id, article.Id);

        await repository.AddAsync(analysis, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var loaded = await repository.GetByIdAsync(analysis.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(analysis.Id, loaded.Id);
        Assert.Equal(user.Id, loaded.UserId);
        Assert.Equal(article.Id, loaded.ArticleId);
        Assert.Equal("Detailed analysis", loaded.AnalysisText);
    }
}