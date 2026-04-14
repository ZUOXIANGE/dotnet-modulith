using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetModulith.Modules.Orders.Infrastructure;

/// <summary>
/// 订单模块数据库上下文设计时工厂，用于EF Core命令行工具生成迁移
/// </summary>
internal sealed class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=modulith_orders;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsAssembly(typeof(OrdersDbContext).Assembly.FullName));

        return new OrdersDbContext(optionsBuilder.Options);
    }
}
