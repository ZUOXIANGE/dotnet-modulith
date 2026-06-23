using DotNetModulith.Modules.Storage.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Storage.Infrastructure;

public sealed class StorageDbContext : DbContext
{
    public DbSet<UploadSessionEntity> UploadSessions => Set<UploadSessionEntity>();

    public StorageDbContext(DbContextOptions<StorageDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("storage");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StorageDbContext).Assembly);
    }
}
