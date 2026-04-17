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

        builder.Ignore(s => s.DomainEvents);

        builder.HasIndex(s => s.ProductId).IsUnique();
    }
}
