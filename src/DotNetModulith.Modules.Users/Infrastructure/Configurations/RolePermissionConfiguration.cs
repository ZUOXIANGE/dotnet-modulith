using DotNetModulith.Modules.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Users.Infrastructure;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(x => new { x.RoleId, x.Permission });
        builder.Property(x => x.RoleId).HasColumnName("role_id");
        builder.Property(x => x.Permission).HasColumnName("permission").HasMaxLength(100).IsRequired();
    }
}
