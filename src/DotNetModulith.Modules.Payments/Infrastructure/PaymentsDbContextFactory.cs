using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetModulith.Modules.Payments.Infrastructure;

/// <summary>
/// 支付模块数据库上下文设计时工厂，用于EF Core命令行工具生成迁移
/// </summary>
internal sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=modulith;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsAssembly(typeof(PaymentsDbContext).Assembly.FullName));

        return new PaymentsDbContext(optionsBuilder.Options);
    }
}
