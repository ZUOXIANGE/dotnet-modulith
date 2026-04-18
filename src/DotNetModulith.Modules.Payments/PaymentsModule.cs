using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Payments.Application.Events;
using DotNetModulith.Modules.Payments.Application.Subscribers;
using DotNetModulith.Modules.Payments.Domain;
using DotNetModulith.Modules.Payments.Domain.Events;
using DotNetModulith.Modules.Payments.Infrastructure;
using DotNetModulith.ModulithCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Payments;

/// <summary>
/// 支付模块定义，声明模块元数据、依赖关系和事件发布/订阅信息
/// </summary>
public sealed class PaymentsModule : IModule
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name => "Payments";

    /// <summary>
    /// 模块基础命名空间
    /// </summary>
    public string BaseNamespace => "DotNetModulith.Modules.Payments";

    /// <summary>
    /// 模块依赖列表（依赖订单模块）
    /// </summary>
    public IReadOnlyList<string> Dependencies => ["Orders"];

    /// <summary>
    /// 模块发布的集成事件列表
    /// </summary>
    public IReadOnlyList<string> PublishedEvents =>
    [
        "modulith.payments.PaymentCompletedIntegrationEvent",
        "modulith.payments.PaymentFailedIntegrationEvent"
    ];

    /// <summary>
    /// 模块订阅的集成事件列表（订阅订单创建事件以发起支付）
    /// </summary>
    public IReadOnlyList<string> SubscribedEvents =>
    [
        "modulith.orders.OrderCreatedIntegrationEvent"
    ];

    /// <summary>
    /// 注册支付模块的数据访问层、仓储和事件处理器
    /// </summary>
    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("modulithdb")
            ?? throw new InvalidOperationException("Connection string 'modulithdb' not found.");

        services.AddDbContext<PaymentsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PaymentsDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddTransient<IDomainEventHandler<PaymentCompletedDomainEvent>, PaymentCompletedDomainEventHandler>();
        services.AddTransient<IDomainEventHandler<PaymentFailedDomainEvent>, PaymentFailedDomainEventHandler>();
        services.AddTransient<OrderEventSubscriber>();

        return services;
    }
}
