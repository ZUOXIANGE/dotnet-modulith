using DotNetModulith.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Orders.Infrastructure;

/// <summary>
/// 订单行项目实体EF Core配置
/// </summary>
internal sealed class OrderLineEntityConfiguration : IEntityTypeConfiguration<OrderLineEntity>
{
    public void Configure(EntityTypeBuilder<OrderLineEntity> builder)
    {
        builder.ToTable("order_lines");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(l => l.ProductId).HasColumnName("product_id").HasMaxLength(100).IsRequired();
        builder.Property(l => l.ProductName).HasColumnName("product_name").HasMaxLength(500).IsRequired();
        builder.Property(l => l.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(l => l.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2).IsRequired();
        builder.Ignore(l => l.LineTotal);

        builder.Property("order_id").HasColumnName("order_id").IsRequired();
    }
}
