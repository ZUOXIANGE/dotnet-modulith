using DotNetModulith.Modules.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Users.Infrastructure;

internal sealed class ModuleUserConfiguration : IEntityTypeConfiguration<ModuleUser>
{
    public void Configure(EntityTypeBuilder<ModuleUser> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.TokenVersion).HasColumnName("token_version").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        builder.HasIndex(x => x.UserName).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasMany(x => x.Roles).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Sessions).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
