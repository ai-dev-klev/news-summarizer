using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Infrastructure.Persistence;

public sealed class DatabaseSeeder
{
    private readonly NewsSummarizerDbContext _context;

    public DatabaseSeeder(NewsSummarizerDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedNewsSourcesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedNewsSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = GetSeedSources();

        foreach (var source in sources)
        {
            var exists = await _context.NewsSources
                .AnyAsync(s => s.Url == source.Url, cancellationToken);

            if (!exists)
                _context.NewsSources.Add(source);
        }
    }

    private static IReadOnlyList<NewsSource> GetSeedSources()
    {
        var now = DateTimeOffset.UtcNow;

        return
        [
            // ── Mock ──────────────────────────────────────────────────────────
            new NewsSource
            {
                Id = Guid.NewGuid(),
                Name = "Mock Source",
                SourceType = SourceType.Mock,
                Url = "mock://source",
                Language = "en",
                DefaultCategories = ["general"],
                IsEnabled = true,
                IsFastSource = false,
                FetchIntervalMinutes = 60,
                TrustScore = 50,
                CreatedAt = now,
                UpdatedAt = now
            },

            // ── General news ──────────────────────────────────────────────────
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

            // ── Technology / business ─────────────────────────────────────────
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

            // ── Research / science / opportunity ──────────────────────────────
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
            },
        ];
    }
}
