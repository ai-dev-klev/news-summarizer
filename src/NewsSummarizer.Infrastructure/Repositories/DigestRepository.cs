using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Infrastructure.Persistence;

namespace NewsSummarizer.Infrastructure.Repositories;

public sealed class DigestRepository : IDigestRepository
{
    private readonly NewsSummarizerDbContext _dbContext;

    public DigestRepository(NewsSummarizerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(
        Guid userId,
        DigestType digestType,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        return _dbContext.Digests.AnyAsync(digest =>
            digest.UserId == userId &&
            digest.DigestType == digestType &&
            digest.PeriodStart == periodStart &&
            digest.PeriodEnd == periodEnd,
            cancellationToken);
    }

    public async Task AddAsync(
        Digest digest,
        IReadOnlyCollection<DigestItem> items,
        CancellationToken cancellationToken)
    {
        await _dbContext.Digests.AddAsync(digest, cancellationToken);

        if (items.Count > 0)
        {
            await _dbContext.DigestItems.AddRangeAsync(items, cancellationToken);
        }
    }

    public Task<Digest?> GetLatestByUserIdAsync(
        Guid userId,
        DigestType digestType,
        CancellationToken cancellationToken)
    {
        return _dbContext.Digests
            .Where(digest =>
                digest.UserId == userId &&
                digest.DigestType == digestType)
            .OrderByDescending(digest => digest.PeriodEnd)
            .ThenByDescending(digest => digest.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DigestItem>> GetItemsAsync(
        Guid digestId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.DigestItems
            .Where(item => item.DigestId == digestId)
            .OrderBy(item => item.Position)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}