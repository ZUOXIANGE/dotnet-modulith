using DotNetModulith.Modules.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Users.Infrastructure;

/// <summary>
/// 用户模块数据库上下文
/// </summary>
public sealed class UsersDbContext : DbContext
{
    public DbSet<ModuleUser> Users => Set<ModuleUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("users");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}
