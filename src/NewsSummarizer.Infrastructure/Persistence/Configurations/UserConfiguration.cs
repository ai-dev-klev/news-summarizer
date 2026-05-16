using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TelegramUserId).IsRequired();

        builder.Property(x => x.Username)
            .HasMaxLength(255);

        builder.Property(x => x.FirstName)
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(UserStatus.Active)
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.TelegramUserId).IsUnique();
    }
}