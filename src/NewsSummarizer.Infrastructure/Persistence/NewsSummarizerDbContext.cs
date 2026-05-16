using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Infrastructure.Persistence;

public sealed class NewsSummarizerDbContext : DbContext
{
    public NewsSummarizerDbContext(DbContextOptions<NewsSummarizerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<NewsSource> NewsSources => Set<NewsSource>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<ArticleAiResult> ArticleAiResults => Set<ArticleAiResult>();
    public DbSet<Digest> Digests => Set<Digest>();
    public DbSet<DigestItem> DigestItems => Set<DigestItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DetailedAnalysis> DetailedAnalyses => Set<DetailedAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.TelegramUserId).IsUnique();
        modelBuilder.Entity<UserPreferences>().HasIndex(x => x.UserId).IsUnique();

        modelBuilder.Entity<NewsSource>().HasIndex(x => x.Url).IsUnique();
        modelBuilder.Entity<NewsSource>().HasIndex(x => x.IsEnabled);
        modelBuilder.Entity<NewsSource>().HasIndex(x => x.IsFastSource);
        modelBuilder.Entity<NewsSource>().Property(x => x.DefaultCategories).HasColumnType("jsonb");

        modelBuilder.Entity<NewsArticle>().HasIndex(x => x.Url).IsUnique();
        modelBuilder.Entity<NewsArticle>().HasIndex(x => x.CanonicalUrl).IsUnique();
        modelBuilder.Entity<NewsArticle>().HasIndex(x => x.SourceId);
        modelBuilder.Entity<NewsArticle>().HasIndex(x => x.NormalizedTitle);
        modelBuilder.Entity<NewsArticle>().HasIndex(x => x.ContentHash);
        modelBuilder.Entity<NewsArticle>().HasIndex(x => x.DedupKey);
        modelBuilder.Entity<NewsArticle>().HasIndex(x => x.Status);
        modelBuilder.Entity<NewsArticle>().HasIndex(x => x.ExpiresAt);

        modelBuilder.Entity<ArticleAiResult>()
            .HasIndex(x => new { x.ArticleId, x.Provider, x.PromptVersion })
            .IsUnique();

        modelBuilder.Entity<Digest>()
            .HasIndex(x => new { x.UserId, x.DigestType, x.PeriodStart, x.PeriodEnd })
            .IsUnique();

        modelBuilder.Entity<DigestItem>()
            .HasIndex(x => new { x.DigestId, x.Position })
            .IsUnique();

        modelBuilder.Entity<Notification>()
            .HasIndex(x => new { x.UserId, x.NotificationType, x.DedupKey })
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<NewsSource>()
            .Property(x => x.SourceType)
            .HasConversion<string>();

        modelBuilder.Entity<NewsArticle>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ArticleAiResult>()
            .Property(x => x.Provider)
            .HasConversion<string>();

        modelBuilder.Entity<ArticleAiResult>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Digest>()
            .Property(x => x.DigestType)
            .HasConversion<string>();

        modelBuilder.Entity<Digest>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(x => x.NotificationType)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<DetailedAnalysis>()
            .Property(x => x.Provider)
            .HasConversion<string>();

        modelBuilder.Entity<DetailedAnalysis>()
            .Property(x => x.Status)
            .HasConversion<string>();
    }
}