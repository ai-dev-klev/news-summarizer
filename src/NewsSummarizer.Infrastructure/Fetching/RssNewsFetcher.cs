using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Infrastructure.Fetching;

public sealed class RssNewsFetcher : INewsFetcher
{
    public Task<IReadOnlyList<FetchedArticle>> FetchAsync(NewsSource source, CancellationToken cancellationToken)
    {
        IReadOnlyList<FetchedArticle> result = [];
        return Task.FromResult(result);
    }
}