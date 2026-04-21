using DotNetModulith.Modules.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Users.Infrastructure;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(x => new { x.UserId, x.RoleId });
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.RoleId).HasColumnName("role_id");
        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}
