using DotNetModulith.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Inventory.Infrastructure;

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
