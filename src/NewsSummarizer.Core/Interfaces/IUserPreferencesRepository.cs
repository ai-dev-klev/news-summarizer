using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Core.Interfaces;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}