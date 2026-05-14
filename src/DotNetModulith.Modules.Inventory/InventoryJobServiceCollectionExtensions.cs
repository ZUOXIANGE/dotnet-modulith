using DotNetModulith.Modules.Inventory.Application.Jobs;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Inventory;

/// <summary>
/// 库存模块后台任务服务注册扩展方法
/// </summary>
public static class InventoryJobServiceCollectionExtensions
{
    /// <summary>
    /// 注册库存模块的后台任务服务（数据库上下文、仓储和低库存告警任务）
    /// </summary>
    public static IServiceCollection AddInventoryJobServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("modulithdb")
            ?? throw new InvalidOperationException("Connection string 'modulithdb' not found.");

        services.AddDbContext<InventoryDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        services.AddOptions<LowStockAlertOptions>()
            .Bind(configuration.GetSection(LowStockAlertOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IStockRepository, StockRepository>();
        services.AddTransient<LowStockAlertJob>();

        return services;
    }
}
