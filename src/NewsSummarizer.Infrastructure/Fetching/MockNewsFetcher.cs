using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Infrastructure.Fetching;

public sealed class MockNewsFetcher : INewsFetcher
{
    public Task<IReadOnlyList<FetchedArticle>> FetchAsync(
        NewsSource source,
        CancellationToken cancellationToken)
    {
        if (source.SourceType != SourceType.Mock)
        {
            return Task.FromResult<IReadOnlyList<FetchedArticle>>([]);
        }

        var now = DateTimeOffset.UtcNow;

        IReadOnlyList<FetchedArticle> result =
        [
            new FetchedArticle(
                "Sample general news title",
                $"https://example.com/news/general-{now:yyyyMMddHHmm}",
                "Sample general description",
                "Sample general content",
                "en",
                now),

            new FetchedArticle(
                "AI startup market grows after new regulation",
                $"https://example.com/news/ai-startup-market-{now:yyyyMMddHHmm}",
                "A mock technology market article for opportunity digest testing.",
                "A mock technology market article that should be treated as an opportunity signal.",
                "en",
                now),

            new FetchedArticle(
                "Urgent market crisis alert from mock source",
                $"https://example.com/news/urgent-market-crisis-{now:yyyyMMddHHmm}",
                "A mock urgent article for future urgent notification testing.",
                "A mock urgent article for future urgent notification testing.",
                "en",
                now)
        ];

        return Task.FromResult(result);
    }
}