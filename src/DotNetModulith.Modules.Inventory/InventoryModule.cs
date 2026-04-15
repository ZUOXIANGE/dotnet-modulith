using System.Diagnostics;
using DotNetModulith.Modules.Inventory.Application.Commands.ReserveStock;
using DotNetModulith.Modules.Inventory.Application.Subscribers;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Infrastructure;
using DotNetModulith.ModulithCore;
using Mediator;
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
    /// 模块依赖列表（无外部依赖）
    /// </summary>
    public IReadOnlyList<string> Dependencies => [];

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
    /// 模块订阅的集成事件列表（订阅订单创建事件以预留库存）
    /// </summary>
    public IReadOnlyList<string> SubscribedEvents =>
    [
        "modulith.orders.OrderCreatedIntegrationEvent"
    ];

    /// <summary>
    /// 注册库存模块的数据访问层、仓储和事件订阅者
    /// </summary>
    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("inventorydb")
            ?? throw new InvalidOperationException("Connection string 'inventorydb' not found.");

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

        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("DotNetModulith.Modules.Inventory"),
            SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        });

        return services;
    }
}
