using DotNetModulith.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Notifications.Infrastructure.Configurations;

public sealed class NotificationEntityConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();

        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RecipientId).HasMaxLength(100).IsRequired();

        builder.Property(x => x.IsRead).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.Property(x => x.ReadAt).IsRequired(false);

        builder.HasIndex(x => x.RecipientId);
        builder.HasIndex(x => new { x.RecipientId, x.IsRead });
        builder.HasIndex(x => x.CreatedAt);
    }
}
