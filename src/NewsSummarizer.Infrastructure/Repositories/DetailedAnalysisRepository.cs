using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Infrastructure.Persistence;

namespace NewsSummarizer.Infrastructure.Repositories;

public sealed class DetailedAnalysisRepository : IDetailedAnalysisRepository
{
    private readonly NewsSummarizerDbContext _dbContext;

    public DetailedAnalysisRepository(NewsSummarizerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DetailedAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.DetailedAnalyses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(DetailedAnalysis analysis, CancellationToken cancellationToken)
    {
        await _dbContext.DetailedAnalyses.AddAsync(analysis, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}