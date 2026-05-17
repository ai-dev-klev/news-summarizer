
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Infrastructure.Persistence.Configurations;

internal sealed class ArticleEmbeddingConfiguration : IEntityTypeConfiguration<ArticleEmbedding>
{
    public void Configure(EntityTypeBuilder<ArticleEmbedding> builder)
    {
        builder.ToTable("article_embeddings");

        builder.HasKey(x => x.ArticleId);

        builder.Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Model)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Dimensions)
            .IsRequired();

        builder.Property(x => x.TextHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Embedding)
            .HasColumnType("real[]")
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.Provider);
        builder.HasIndex(x => x.Model);
        builder.HasIndex(x => x.TextHash);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne<NewsArticle>()
            .WithMany()
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
