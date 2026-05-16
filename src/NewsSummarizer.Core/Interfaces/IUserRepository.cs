using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}