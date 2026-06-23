using DotNetModulith.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Notifications.Infrastructure;

public sealed class NotificationsDbContext : DbContext
{
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
