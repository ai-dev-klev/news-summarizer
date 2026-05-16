using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Infrastructure.Fetching;

public sealed class MockNewsFetcher : INewsFetcher
{
    public Task<IReadOnlyList<FetchedArticle>> FetchAsync(NewsSource source, CancellationToken cancellationToken)
    {
        IReadOnlyList<FetchedArticle> result =
        [
            new FetchedArticle(
                "Sample news title",
                "https://example.com/news",
                "Sample description",
                "Sample content",
                "en",
                DateTimeOffset.UtcNow)
        ];

        return Task.FromResult(result);
    }
}