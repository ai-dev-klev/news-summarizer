using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Tests;

public sealed class DomainDefaultsTests
{
    [Fact]
    public void NewArticle_ShouldHaveNewStatusByDefault()
    {
        var article = new NewsArticle();

        Assert.Equal(ArticleStatus.New, article.Status);
    }

    [Fact]
    public void NewArticleAiResult_ShouldHavePendingStatusAndPromptVersionByDefault()
    {
        var result = new ArticleAiResult();

        Assert.Equal(AiResultStatus.Pending, result.Status);
        Assert.Equal("v1", result.PromptVersion);
    }

    [Fact]
    public void NewNotification_ShouldHavePendingStatusByDefault()
    {
        var notification = new Notification();

        Assert.Equal(NotificationStatus.Pending, notification.Status);
    }

    [Fact]
    public void NewUserPreferences_ShouldEnableMainNotificationsByDefault()
    {
        var preferences = new UserPreferences();

        Assert.True(preferences.DailyDigestEnabled);
        Assert.True(preferences.OpportunityDigestEnabled);
        Assert.True(preferences.UrgentNotificationsEnabled);
        Assert.Equal(10, preferences.MaxItemsPerDigest);
        Assert.Equal("Europe/Moscow", preferences.Timezone);
        Assert.Empty(preferences.EnabledCategories);
        Assert.Empty(preferences.UrgentTopics);
    }

    [Fact]
    public void NewNewsSource_ShouldHaveSafeDefaults()
    {
        var source = new NewsSource();

        Assert.True(source.IsEnabled);
        Assert.Equal(60, source.FetchIntervalMinutes);
        Assert.Equal(50, source.TrustScore);
        Assert.Empty(source.DefaultCategories);
    }
}