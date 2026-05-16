using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Core.Interfaces;

public interface IDetailedAnalysisRepository
{
    Task<DetailedAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(DetailedAnalysis analysis, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}