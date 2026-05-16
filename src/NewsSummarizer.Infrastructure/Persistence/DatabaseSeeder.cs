using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Infrastructure.Persistence;

public sealed class DatabaseSeeder
{
    private readonly NewsSummarizerDbContext _dbContext;

    public DatabaseSeeder(NewsSummarizerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        if (!await _dbContext.NewsSources.AnyAsync(cancellationToken))
        {
            var source = new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "Mock source",
                SourceType = SourceType.Mock,
                Url = "mock://news",
                Language = "en",
                DefaultCategories = ["general", "technology"],
                IsEnabled = true,
                IsFastSource = true,
                FetchIntervalMinutes = 60,
                TrustScore = 50,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _dbContext.NewsSources.AddAsync(source, cancellationToken);
        }

        if (!await _dbContext.Users.AnyAsync(cancellationToken))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                TelegramUserId = 1,
                Username = "demo_user",
                FirstName = "Demo",
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };

            var preferences = new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EnabledCategories = ["general", "technology", "business", "sport"],
                UrgentTopics = ["war", "pandemic", "market_crash", "critical_event"],
                ImportantTopicsText = "technology, business, sport, important local events",
                ExcludedTopicsText = "clickbait",
                DailyDigestEnabled = true,
                DailyDigestTime = new TimeOnly(9, 0),
                OpportunityDigestEnabled = true,
                OpportunityDigestTime = new TimeOnly(18, 0),
                UrgentNotificationsEnabled = true,
                MaxItemsPerDigest = 10,
                Timezone = "Europe/Moscow",
                CreatedAt = now,
                UpdatedAt = now
            };

            await _dbContext.Users.AddAsync(user, cancellationToken);
            await _dbContext.UserPreferences.AddAsync(preferences, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}