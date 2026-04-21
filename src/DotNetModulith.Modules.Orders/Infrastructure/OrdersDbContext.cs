using DotNetModulith.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}
