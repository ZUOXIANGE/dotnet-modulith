using DotNetModulith.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetModulith.Modules.Orders.Infrastructure;

/// <summary>
/// 订单模块数据库上下文
/// </summary>
public sealed class OrdersDbContext : DbContext
{
    /// <summary>
    /// 订单数据集
    /// </summary>
    public DbSet<Order> Orders => Set<Order>();

    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Detached)
                continue;
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}

/// <summary>
/// 订单实体EF Core配置
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new OrderId(value));

        builder.Property(o => o.CustomerId)
            .HasColumnName("customer_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Ignore(o => o.DomainEvents);
        builder.Ignore(o => o.TotalAmount);
        builder.Ignore(o => o.Lines);

        builder.HasMany("_lines")
            .WithOne()
            .HasForeignKey("order_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.Status);
    }
}

/// <summary>
/// 订单行项目实体EF Core配置
/// </summary>
internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
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
