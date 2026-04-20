using DotNetModulith.JobHost.Infrastructure;
using DotNetModulith.Modules.Inventory.Infrastructure;
using DotNetModulith.Modules.Orders.Infrastructure;
using DotNetModulith.Modules.Payments.Infrastructure;
using DotNetModulith.Modules.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("modulithdb")
    ?? throw new InvalidOperationException("modulithdb connection string not found.");
var tickerQConnectionString = builder.Configuration.GetConnectionString("tickerqdb")
    ?? throw new InvalidOperationException("tickerqdb connection string not found.");

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<TickerQSchedulerDbContext>(options =>
    options.UseNpgsql(
        tickerQConnectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(TickerQSchedulerDbContext).Assembly.FullName)));

builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();
host.Run();

/// <summary>
/// 数据库迁移后台服务，在应用启动时自动执行待处理的EF Core迁移
/// </summary>
internal sealed class MigrationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<MigrationWorker> _logger;

    public MigrationWorker(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<MigrationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Migration service starting for {Application}", "DotNetModulith");

        try
        {
            using var scope = _serviceProvider.CreateScope();

            var ordersDb = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var inventoryDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            var usersDb = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var tickerQDb = scope.ServiceProvider.GetRequiredService<TickerQSchedulerDbContext>();

            await MigrateAsync(ordersDb, "Orders", stoppingToken);
            await MigrateAsync(inventoryDb, "Inventory", stoppingToken);
            await MigrateAsync(paymentsDb, "Payments", stoppingToken);
            await MigrateAsync(usersDb, "Users", stoppingToken);
            await MigrateAsync(tickerQDb, "TickerQ", stoppingToken);

            _logger.LogInformation("All migrations applied successfully for {ModuleCount} modules", 5);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration service failed: {Error}", ex.Message);
            throw;
        }
        finally
        {
            _hostApplicationLifetime.StopApplication();
        }
    }

    /// <summary>
    /// 对指定数据库上下文执行迁移，使用执行策略确保在数据库暂不可用时重试
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="name">模块名称（用于日志）</param>
    /// <param name="ct">取消令牌</param>
    private async Task MigrateAsync(DbContext dbContext, string name, CancellationToken ct)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            var pending = await dbContext.Database.GetPendingMigrationsAsync(ct);

            if (pending.Any())
            {
                _logger.LogInformation("Applying {Count} pending migrations for {Module}...",
                    pending.Count(), name);
                await dbContext.Database.MigrateAsync(ct);
                _logger.LogInformation("{Module} migrations applied.", name);
            }
            else
            {
                _logger.LogInformation("{Module} database is up to date.", name);
            }
        });
    }
}
