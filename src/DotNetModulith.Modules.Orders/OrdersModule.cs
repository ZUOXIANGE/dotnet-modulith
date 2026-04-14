using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Orders;

/// <summary>
/// 订单模块定义，声明模块元数据、依赖关系和事件发布/订阅信息
/// </summary>
public sealed class OrdersModule : IModule
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name => "Orders";

    /// <summary>
    /// 模块基础命名空间
    /// </summary>
    public string BaseNamespace => "DotNetModulith.Modules.Orders";

    /// <summary>
    /// 模块依赖列表（依赖库存模块）
    /// </summary>
    public IReadOnlyList<string> Dependencies => ["Inventory"];

    /// <summary>
    /// 模块发布的集成事件列表
    /// </summary>
    public IReadOnlyList<string> PublishedEvents =>
    [
        "modulith.orders.OrderCreatedIntegrationEvent",
        "modulith.orders.OrderPaidIntegrationEvent",
        "modulith.orders.OrderCancelledIntegrationEvent"
    ];

    /// <summary>
    /// 模块订阅的集成事件列表
    /// </summary>
    public IReadOnlyList<string> SubscribedEvents =>
    [
        "modulith.inventory.StockReservedIntegrationEvent",
        "modulith.payments.PaymentCompletedIntegrationEvent"
    ];

    /// <summary>
    /// 注册订单模块的基础设施和应用层服务
    /// </summary>
    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOrdersInfrastructure(configuration);
        services.AddOrdersApplication();
        return services;
    }
}
