using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetModulith.Modules.Inventory.Infrastructure;

/// <summary>
/// 库存模块数据库上下文设计时工厂，用于EF Core命令行工具生成迁移
/// </summary>
internal sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=modulith_inventory;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName));

        return new InventoryDbContext(optionsBuilder.Options);
    }
}
