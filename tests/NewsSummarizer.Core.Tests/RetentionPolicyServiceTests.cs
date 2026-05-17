using NewsSummarizer.Core.Services;

namespace NewsSummarizer.Core.Tests;

public sealed class RetentionPolicyServiceTests
{
    private readonly RetentionPolicyService _service = new();

    [Fact]
    public void GetArticleExpiresAt_ShouldReturnNowPlus14Days()
    {
        var now = new DateTimeOffset(2026, 5, 17, 12, 30, 0, TimeSpan.Zero);

        var result = _service.GetArticleExpiresAt(now);

        Assert.Equal(now.AddDays(14), result);
    }

    [Fact]
    public void GetNotificationExpiresAt_ShouldReturnNowPlus30Days()
    {
        var now = new DateTimeOffset(2026, 5, 17, 12, 30, 0, TimeSpan.Zero);

        var result = _service.GetNotificationExpiresAt(now);

        Assert.Equal(now.AddDays(30), result);
    }

    [Fact]
    public void GetDetailedAnalysisExpiresAt_ShouldReturnNowPlus30Days()
    {
        var now = new DateTimeOffset(2026, 5, 17, 12, 30, 0, TimeSpan.Zero);

        var result = _service.GetDetailedAnalysisExpiresAt(now);

        Assert.Equal(now.AddDays(30), result);
    }

    [Fact]
    public void RetentionPolicy_ShouldPreserveOriginalOffset()
    {
        var now = new DateTimeOffset(2026, 5, 17, 12, 30, 0, TimeSpan.FromHours(3));

        Assert.Equal(TimeSpan.FromHours(3), _service.GetArticleExpiresAt(now).Offset);
        Assert.Equal(TimeSpan.FromHours(3), _service.GetNotificationExpiresAt(now).Offset);
        Assert.Equal(TimeSpan.FromHours(3), _service.GetDetailedAnalysisExpiresAt(now).Offset);
    }
}