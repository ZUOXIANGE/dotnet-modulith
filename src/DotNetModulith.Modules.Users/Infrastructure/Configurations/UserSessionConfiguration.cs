using DotNetModulith.Modules.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Users.Infrastructure;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(x => x.TokenId);
        builder.Property(x => x.TokenId).HasColumnName("token_id").HasMaxLength(64).IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.RemoteIp).HasColumnName("remote_ip").HasMaxLength(100);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(1000);
        builder.Property(x => x.IssuedAt).HasColumnName("issued_at").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Property(x => x.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(200);
        builder.HasIndex(x => new { x.UserId, x.RevokedAt });
    }
}
