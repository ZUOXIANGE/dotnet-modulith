using DotNetModulith.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Inventory.Infrastructure;

/// <summary>
/// 库存实体EF Core配置
/// </summary>
internal sealed class StockEntityConfiguration : IEntityTypeConfiguration<StockEntity>
{
    public void Configure(EntityTypeBuilder<StockEntity> builder)
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
