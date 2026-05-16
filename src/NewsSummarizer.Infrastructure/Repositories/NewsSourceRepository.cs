using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Infrastructure.Persistence;

namespace NewsSummarizer.Infrastructure.Repositories;

public sealed class NewsSourceRepository : INewsSourceRepository
{
    private readonly NewsSummarizerDbContext _dbContext;

    public NewsSourceRepository(NewsSummarizerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<NewsSource>> GetEnabledAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.NewsSources
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NewsSource>> GetEnabledFastSourcesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.NewsSources
            .Where(x => x.IsEnabled && x.IsFastSource)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<NewsSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.NewsSources.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(NewsSource source, CancellationToken cancellationToken)
    {
        await _dbContext.NewsSources.AddAsync(source, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}