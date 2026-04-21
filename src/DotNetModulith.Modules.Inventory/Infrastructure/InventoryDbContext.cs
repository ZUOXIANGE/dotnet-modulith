using DotNetModulith.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Inventory.Infrastructure;

/// <summary>
/// 库存模块数据库上下文
/// </summary>
public sealed class InventoryDbContext : DbContext
{
    /// <summary>
    /// 库存数据集
    /// </summary>
    public DbSet<StockEntity> Stocks => Set<StockEntity>();

    /// <summary>
    /// 库存预留明细数据集
    /// </summary>
    public DbSet<StockReservationEntity> StockReservations => Set<StockReservationEntity>();

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
