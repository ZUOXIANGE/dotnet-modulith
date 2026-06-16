using DotNetModulith.Modules.Fines.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Fines.Infrastructure;

public sealed class FinesDbContext : DbContext
{
    public DbSet<FineEntity> Fines => Set<FineEntity>();

    public FinesDbContext(DbContextOptions<FinesDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("fines");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinesDbContext).Assembly);
    }
}
