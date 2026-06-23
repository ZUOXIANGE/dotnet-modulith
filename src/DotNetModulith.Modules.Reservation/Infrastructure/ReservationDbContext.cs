using DotNetModulith.Modules.Reservation.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Reservation.Infrastructure;

public sealed class ReservationDbContext : DbContext
{
    public DbSet<ReservationEntity> Reservations => Set<ReservationEntity>();

    public ReservationDbContext(DbContextOptions<ReservationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reservation");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReservationDbContext).Assembly);
    }
}
