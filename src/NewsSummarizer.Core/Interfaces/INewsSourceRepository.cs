using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Core.Interfaces;

public interface INewsSourceRepository
{
    Task<IReadOnlyList<NewsSource>> GetEnabledAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<NewsSource>> GetEnabledFastSourcesAsync(CancellationToken cancellationToken);
    Task<NewsSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(NewsSource source, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}