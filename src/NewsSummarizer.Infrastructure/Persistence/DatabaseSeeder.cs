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

        await SeedNewsSourcesAsync(now, cancellationToken);
        await SeedDemoUserAsync(now, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedNewsSourcesAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var source in GetSeedSources(now))
        {
            var exists = await _dbContext.NewsSources
                .AnyAsync(existing => existing.Url == source.Url, cancellationToken);

            if (!exists)
            {
                await _dbContext.NewsSources.AddAsync(source, cancellationToken);
            }
        }
    }

    private async Task SeedDemoUserAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(existing => existing.TelegramUserId == 1, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                TelegramUserId = 1,
                Username = "demo_user",
                FirstName = "Demo",
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _dbContext.Users.AddAsync(user, cancellationToken);
        }

        var preferences = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(existing => existing.UserId == user.Id, cancellationToken);

        if (preferences is null)
        {
            preferences = new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EnabledCategories =
                [
                    "general",
                    "technology",
                    "business",
                    "sport",
                    "science",
                    "research",
                    "startups",
                    "market"
                ],
                UrgentTopics =
                [
                    "war",
                    "pandemic",
                    "market_crash",
                    "critical_event"
                ],
                ImportantTopicsText = "technology, business, sport, science, research, startups, market, important local events",
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

            await _dbContext.UserPreferences.AddAsync(preferences, cancellationToken);
        }
    }

    private static IReadOnlyList<NewsSource> GetSeedSources(DateTimeOffset now)
    {
        return
        [
            new NewsSource
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
            },

            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "NPR News",
                SourceType = SourceType.Rss,
                Url = "https://feeds.npr.org/1001/rss.xml",
                Language = "en",
                DefaultCategories = ["general", "world", "politics"],
                IsEnabled = true,
                IsFastSource = true,
                FetchIntervalMinutes = 30,
                TrustScore = 80,
                CreatedAt = now,
                UpdatedAt = now
            },
            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "ProPublica",
                SourceType = SourceType.Rss,
                Url = "https://feeds.propublica.org/propublica/main",
                Language = "en",
                DefaultCategories = ["general", "politics", "business"],
                IsEnabled = true,
                IsFastSource = false,
                FetchIntervalMinutes = 60,
                TrustScore = 85,
                CreatedAt = now,
                UpdatedAt = now
            },
            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "The Moscow Times",
                SourceType = SourceType.Rss,
                Url = "https://www.themoscowtimes.com/rss/news",
                Language = "en",
                DefaultCategories = ["general", "politics"],
                IsEnabled = true,
                IsFastSource = false,
                FetchIntervalMinutes = 60,
                TrustScore = 75,
                CreatedAt = now,
                UpdatedAt = now
            },
            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "Meduza",
                SourceType = SourceType.Rss,
                Url = "https://meduza.io/rss/all",
                Language = "ru",
                DefaultCategories = ["general", "politics"],
                IsEnabled = true,
                IsFastSource = true,
                FetchIntervalMinutes = 30,
                TrustScore = 80,
                CreatedAt = now,
                UpdatedAt = now
            },
            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "BBC News",
                SourceType = SourceType.Rss,
                Url = "https://feeds.bbci.co.uk/news/rss.xml",
                Language = "en",
                DefaultCategories = ["general", "world", "politics"],
                IsEnabled = true,
                IsFastSource = true,
                FetchIntervalMinutes = 30,
                TrustScore = 85,
                CreatedAt = now,
                UpdatedAt = now
            },
            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "Reuters Top News",
                SourceType = SourceType.Rss,
                Url = "https://feeds.reuters.com/reuters/topNews",
                Language = "en",
                DefaultCategories = ["general", "world", "business"],
                IsEnabled = true,
                IsFastSource = true,
                FetchIntervalMinutes = 15,
                TrustScore = 90,
                CreatedAt = now,
                UpdatedAt = now
            },

            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "WIRED Top Stories",
                SourceType = SourceType.Rss,
                Url = "https://www.wired.com/feed/rss",
                Language = "en",
                DefaultCategories = ["technology", "science", "startups"],
                IsEnabled = true,
                IsFastSource = false,
                FetchIntervalMinutes = 60,
                TrustScore = 75,
                CreatedAt = now,
                UpdatedAt = now
            },
            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "TechCrunch Startups",
                SourceType = SourceType.Rss,
                Url = "https://techcrunch.com/category/startups/feed/",
                Language = "en",
                DefaultCategories = ["startups", "technology", "business"],
                IsEnabled = true,
                IsFastSource = false,
                FetchIntervalMinutes = 60,
                TrustScore = 75,
                CreatedAt = now,
                UpdatedAt = now
            },
            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "Hacker News (Show HN)",
                SourceType = SourceType.Rss,
                Url = "https://hnrss.org/show",
                Language = "en",
                DefaultCategories = ["technology", "startups"],
                IsEnabled = true,
                IsFastSource = false,
                FetchIntervalMinutes = 60,
                TrustScore = 65,
                CreatedAt = now,
                UpdatedAt = now
            },

            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "arXiv cs.AI",
                SourceType = SourceType.Rss,
                Url = "https://rss.arxiv.org/rss/cs.AI",
                Language = "en",
                DefaultCategories = ["research", "technology", "science"],
                IsEnabled = true,
                IsFastSource = false,
                FetchIntervalMinutes = 360,
                TrustScore = 80,
                CreatedAt = now,
                UpdatedAt = now
            },
            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "ScienceDaily",
                SourceType = SourceType.Rss,
                Url = "https://www.sciencedaily.com/rss/all.xml",
                Language = "en",
                DefaultCategories = ["research", "science", "health", "technology"],
                IsEnabled = true,
                IsFastSource = false,
                FetchIntervalMinutes = 120,
                TrustScore = 75,
                CreatedAt = now,
                UpdatedAt = now
            },
            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "MIT News Research",
                SourceType = SourceType.Rss,
                Url = "https://news.mit.edu/rss/research",
                Language = "en",
                DefaultCategories = ["research", "technology", "science"],
                IsEnabled = true,
                IsFastSource = false,
                FetchIntervalMinutes = 120,
                TrustScore = 85,
                CreatedAt = now,
                UpdatedAt = now
            }
        ];
    }
}