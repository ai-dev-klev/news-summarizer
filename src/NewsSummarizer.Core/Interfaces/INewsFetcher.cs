using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.Interfaces;

public interface INewsFetcher
{
    Task<IReadOnlyList<FetchedArticle>> FetchAsync(NewsSource source, CancellationToken cancellationToken);
}