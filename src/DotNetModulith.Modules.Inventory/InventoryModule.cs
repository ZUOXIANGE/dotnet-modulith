using DotNetModulith.Modules.Inventory.Application.Subscribers;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Infrastructure;
using DotNetModulith.ModulithCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Inventory;

/// <summary>
/// 库存模块定义，声明模块元数据、依赖关系和事件发布/订阅信息
/// </summary>
public sealed class InventoryModule : IModule
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name => "Inventory";

    /// <summary>
    /// 模块基础命名空间
    /// </summary>
    public string BaseNamespace => "DotNetModulith.Modules.Inventory";

    /// <summary>
    /// 模块依赖列表（依赖订单模块事件）
    /// </summary>
    public IReadOnlyList<string> Dependencies => ["Orders"];

    /// <summary>
    /// 模块发布的集成事件列表
    /// </summary>
    public IReadOnlyList<string> PublishedEvents =>
    [
        "modulith.inventory.StockReservedIntegrationEvent",
        "modulith.inventory.StockInsufficientIntegrationEvent",
        "modulith.inventory.StockReplenishedIntegrationEvent"
    ];

    /// <summary>
    /// 模块订阅的集成事件列表（订阅订单创建、取消、支付完成事件）
    /// </summary>
    public IReadOnlyList<string> SubscribedEvents =>
    [
        "modulith.orders.OrderCreatedIntegrationEvent",
        "modulith.orders.OrderCancelledIntegrationEvent",
        "modulith.orders.OrderPaidIntegrationEvent"
    ];

    /// <summary>
    /// 注册库存模块的数据访问层、仓储和事件订阅者
    /// </summary>
    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
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

        services.AddScoped<IStockRepository, StockRepository>();
        services.AddTransient<OrderEventSubscriber>();

        return services;
    }
}
