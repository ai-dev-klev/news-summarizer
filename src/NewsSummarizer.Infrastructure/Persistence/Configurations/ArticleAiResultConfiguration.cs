using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Infrastructure.Persistence.Configurations;

internal sealed class ArticleAiResultConfiguration : IEntityTypeConfiguration<ArticleAiResult>
{
    public void Configure(EntityTypeBuilder<ArticleAiResult> builder)
    {
        builder.ToTable("article_ai_results", table =>
        {
            table.HasCheckConstraint(
                "ck_article_ai_results_importance_score_range",
                "importance_score >= 0 AND importance_score <= 100");

            table.HasCheckConstraint(
                "ck_article_ai_results_urgency_score_range",
                "urgency_score >= 0 AND urgency_score <= 100");

            table.HasCheckConstraint(
                "ck_article_ai_results_opportunity_score_range",
                "opportunity_score >= 0 AND opportunity_score <= 100");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ArticleId).IsRequired();

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

        builder.Property(x => x.Category)
            .HasMaxLength(100);

        builder.Property(x => x.ImportanceScore).IsRequired();
        builder.Property(x => x.UrgencyScore).IsRequired();
        builder.Property(x => x.OpportunityScore).IsRequired();

        builder.Property(x => x.DailyDigestCandidate)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.OpportunityDigestCandidate)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.UrgentCandidate)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(AiResultStatus.Pending)
            .IsRequired();

        builder.Property(x => x.RawResponseJson)
            .HasColumnName("raw_response")
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.ArticleId);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.ImportanceScore);
        builder.HasIndex(x => x.UrgencyScore);
        builder.HasIndex(x => x.OpportunityScore);
        builder.HasIndex(x => x.UrgentCandidate);
        builder.HasIndex(x => x.DailyDigestCandidate);
        builder.HasIndex(x => x.OpportunityDigestCandidate);

        builder.HasIndex(x => new { x.ArticleId, x.Provider, x.PromptVersion }).IsUnique();

        builder.HasOne<NewsArticle>()
            .WithMany()
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}