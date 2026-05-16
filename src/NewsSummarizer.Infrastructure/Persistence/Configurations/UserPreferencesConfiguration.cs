using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Infrastructure.Persistence.Configurations;

internal sealed class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("user_preferences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.EnabledCategories)
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConverters.StringListConverter);
        builder.Property(x => x.EnabledCategories).Metadata.SetValueComparer(JsonValueConverters.StringListComparer);

        builder.Property(x => x.UrgentTopics)
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConverters.StringListConverter);
        builder.Property(x => x.UrgentTopics).Metadata.SetValueComparer(JsonValueConverters.StringListComparer);

        builder.Property(x => x.ImportantTopicsText);
        builder.Property(x => x.ExcludedTopicsText);

        builder.Property(x => x.DailyDigestEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.OpportunityDigestEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.UrgentNotificationsEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.MaxItemsPerDigest)
            .HasDefaultValue(10)
            .IsRequired();

        builder.Property(x => x.Timezone)
            .HasMaxLength(100)
            .HasDefaultValue("Europe/Moscow")
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}