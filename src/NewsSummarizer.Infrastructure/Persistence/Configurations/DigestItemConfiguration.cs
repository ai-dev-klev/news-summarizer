using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Infrastructure.Persistence.Configurations;

internal sealed class DigestItemConfiguration : IEntityTypeConfiguration<DigestItem>
{
    public void Configure(EntityTypeBuilder<DigestItem> builder)
    {
        builder.ToTable("digest_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DigestId).IsRequired();
        builder.Property(x => x.Position).IsRequired();

        builder.Property(x => x.TitleSnapshot).IsRequired();

        builder.Property(x => x.SourceNameSnapshot)
            .HasMaxLength(255);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.DigestId);
        builder.HasIndex(x => x.ArticleId);
        builder.HasIndex(x => new { x.DigestId, x.Position }).IsUnique();

        builder.HasOne<Digest>()
            .WithMany()
            .HasForeignKey(x => x.DigestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<NewsArticle>()
            .WithMany()
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}