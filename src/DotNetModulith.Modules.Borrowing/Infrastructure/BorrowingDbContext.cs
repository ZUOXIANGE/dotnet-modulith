using DotNetModulith.Modules.Borrowing.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Borrowing.Infrastructure;

public sealed class BorrowingDbContext : DbContext
{
    public DbSet<BorrowingRecordEntity> BorrowingRecords => Set<BorrowingRecordEntity>();

    public BorrowingDbContext(DbContextOptions<BorrowingDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("borrowing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BorrowingDbContext).Assembly);
    }
}
