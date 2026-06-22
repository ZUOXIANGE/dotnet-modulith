using DotNetModulith.Modules.Storage.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Storage.Infrastructure.Configurations;

internal sealed class UploadSessionEntityConfiguration : IEntityTypeConfiguration<UploadSessionEntity>
{
    public void Configure(EntityTypeBuilder<UploadSessionEntity> builder)
    {
        builder.ToTable("upload_sessions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
        builder.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(50).IsRequired();
        builder.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.DeclaredContentType).HasColumnName("declared_content_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ObjectKey).HasColumnName("object_key").HasMaxLength(500).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at");

        builder.HasIndex(x => x.OwnerUserId);
        builder.HasIndex(x => new { x.Purpose, x.Status });
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => x.ObjectKey).IsUnique();
    }
}
