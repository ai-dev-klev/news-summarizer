using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Infrastructure.Persistence.Configurations;

internal sealed class NewsSourceConfiguration : IEntityTypeConfiguration<NewsSource>
{
    public void Configure(EntityTypeBuilder<NewsSource> builder)
    {
        builder.ToTable("news_sources");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.SourceType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Url)
            .IsRequired();

        builder.Property(x => x.Language)
            .HasMaxLength(20);

        builder.Property(x => x.DefaultCategories)
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConverters.StringListConverter);
        builder.Property(x => x.DefaultCategories).Metadata.SetValueComparer(JsonValueConverters.StringListComparer);

        builder.Property(x => x.IsEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsFastSource)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.FetchIntervalMinutes)
            .HasDefaultValue(60)
            .IsRequired();

        builder.Property(x => x.TrustScore)
            .HasDefaultValue(50)
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.Url).IsUnique();
        builder.HasIndex(x => x.IsEnabled);
        builder.HasIndex(x => x.IsFastSource);
        builder.HasIndex(x => x.Language);
    }
}