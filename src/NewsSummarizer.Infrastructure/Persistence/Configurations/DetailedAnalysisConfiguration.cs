using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Infrastructure.Persistence.Configurations;

internal sealed class DetailedAnalysisConfiguration : IEntityTypeConfiguration<DetailedAnalysis>
{
    public void Configure(EntityTypeBuilder<DetailedAnalysis> builder)
    {
        builder.ToTable("detailed_analyses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Model)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.PromptVersion)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RawResponseJson)
            .HasColumnName("raw_response")
            .HasColumnType("jsonb");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(AiResultStatus.Pending)
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ArticleId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExpiresAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<NewsArticle>()
            .WithMany()
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}