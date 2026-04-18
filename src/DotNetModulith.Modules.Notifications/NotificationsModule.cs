using DotNetModulith.Modules.Notifications.Application.Subscribers;
using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Notifications;

/// <summary>
/// 通知模块定义，声明模块元数据、依赖关系和事件发布/订阅信息
/// </summary>
public sealed class NotificationsModule : IModule
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name => "Notifications";

    /// <summary>
    /// 模块基础命名空间
    /// </summary>
    public string BaseNamespace => "DotNetModulith.Modules.Notifications";

    /// <summary>
    /// 模块依赖列表（依赖订单和支付模块）
    /// </summary>
    public IReadOnlyList<string> Dependencies => ["Orders", "Payments"];

    /// <summary>
    /// 模块发布的集成事件列表（通知模块不发布事件）
    /// </summary>
    public IReadOnlyList<string> PublishedEvents => [];

    /// <summary>
    /// 模块订阅的集成事件列表（订阅订单和支付相关事件以发送通知）
    /// </summary>
    public IReadOnlyList<string> SubscribedEvents =>
    [
        "modulith.orders.OrderCreatedIntegrationEvent",
        "modulith.payments.PaymentCompletedIntegrationEvent",
        "modulith.orders.OrderCancelledIntegrationEvent"
    ];

    /// <summary>
    /// 注册通知模块的事件订阅者
    /// </summary>
    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<NotificationEventSubscriber>();

        return services;
    }
}
