using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Infrastructure.Persistence.Configurations;

internal sealed class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.ToTable("news_articles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceId).IsRequired();

        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Url).IsRequired();
        builder.Property(x => x.CanonicalUrl);

        builder.Property(x => x.Language)
            .HasMaxLength(20);

        builder.Property(x => x.FetchedAt).IsRequired();

        builder.Property(x => x.NormalizedTitle).IsRequired();

        builder.Property(x => x.ContentHash)
            .HasMaxLength(128);

        builder.Property(x => x.DedupKey)
            .HasMaxLength(512);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ArticleStatus.New)
            .IsRequired();

        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.Url).IsUnique();
        builder.HasIndex(x => x.CanonicalUrl).IsUnique();
        builder.HasIndex(x => x.SourceId);
        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => x.FetchedAt);
        builder.HasIndex(x => x.NormalizedTitle);
        builder.HasIndex(x => x.ContentHash);
        builder.HasIndex(x => x.DedupKey);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => x.Language);

        builder.HasOne<NewsSource>()
            .WithMany()
            .HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<NewsArticle>()
            .WithMany()
            .HasForeignKey(x => x.DuplicateOfArticleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}