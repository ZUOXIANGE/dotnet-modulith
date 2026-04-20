using DotNetModulith.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Inventory.Infrastructure;

/// <summary>
/// 库存模块数据库上下文
/// </summary>
public sealed class InventoryDbContext : DbContext
{
    /// <summary>
    /// 库存数据集
    /// </summary>
    public DbSet<Stock> Stocks => Set<Stock>();

    /// <summary>
    /// 库存预留明细数据集
    /// </summary>
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

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

/// <summary>
/// 库存实体EF Core配置
/// </summary>
internal sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("stocks");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new StockId(value));

        builder.Property(s => s.ProductId)
            .HasColumnName("product_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.AvailableQuantity)
            .HasColumnName("available_quantity")
            .IsRequired();

        builder.Property(s => s.ReservedQuantity)
            .HasColumnName("reserved_quantity")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(s => s.LowStockAlertSentAt)
            .HasColumnName("low_stock_alert_sent_at");

        builder.Property(s => s.LastAlertedAvailableQuantity)
            .HasColumnName("last_alerted_available_quantity");

        builder.Ignore(s => s.DomainEvents);

        builder.HasIndex(s => s.ProductId).IsUnique();
    }
}

/// <summary>
/// 库存预留明细EF Core配置
/// </summary>
internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.StockId)
            .HasColumnName("stock_id")
            .HasConversion(
                id => id.Value,
                value => new StockId(value))
            .IsRequired();

        builder.Property(r => r.OrderId)
            .HasColumnName("order_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.ProductId)
            .HasColumnName("product_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.CompletedAt)
            .HasColumnName("completed_at");

        builder.HasIndex(r => new { r.OrderId, r.ProductId }).IsUnique();
        builder.HasIndex(r => new { r.OrderId, r.Status });
    }
}
