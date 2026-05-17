using Microsoft.EntityFrameworkCore;
using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Infrastructure.Persistence;

public sealed class NewsSummarizerDbContext : DbContext
{
    public NewsSummarizerDbContext(DbContextOptions<NewsSummarizerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<NewsSource> NewsSources => Set<NewsSource>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<ArticleAiResult> ArticleAiResults => Set<ArticleAiResult>();
    public DbSet<ArticleEmbedding> ArticleEmbeddings => Set<ArticleEmbedding>();
    public DbSet<Digest> Digests => Set<Digest>();
    public DbSet<DigestItem> DigestItems => Set<DigestItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DetailedAnalysis> DetailedAnalyses => Set<DetailedAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NewsSummarizerDbContext).Assembly);
    }
}
