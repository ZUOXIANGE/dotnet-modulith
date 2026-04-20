using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.Utilities.Entities;

namespace DotNetModulith.JobHost.Infrastructure;

/// <summary>
/// TickerQ 独立调度数据库上下文。
/// 与业务模块数据库隔离，避免调度表进入业务库。
/// </summary>
public sealed class TickerQSchedulerDbContext : TickerQDbContext<TimeTickerEntity, CronTickerEntity>
{
    public TickerQSchedulerDbContext(DbContextOptions<TickerQSchedulerDbContext> options)
        : base(options)
    {
    }
}

/// <summary>
/// TickerQ 调度数据库设计时工厂，用于生成和维护调度库迁移。
/// </summary>
internal sealed class TickerQSchedulerDbContextFactory : IDesignTimeDbContextFactory<TickerQSchedulerDbContext>
{
    public TickerQSchedulerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TickerQSchedulerDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=tickerq;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsAssembly(typeof(TickerQSchedulerDbContext).Assembly.FullName));

        return new TickerQSchedulerDbContext(optionsBuilder.Options);
    }
}
