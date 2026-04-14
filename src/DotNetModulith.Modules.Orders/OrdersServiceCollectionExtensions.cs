using System.Diagnostics;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Orders.Application.Events;
using DotNetModulith.Modules.Orders.Application.Subscribers;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Domain.Events;
using DotNetModulith.Modules.Orders.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Orders;

/// <summary>
/// 订单模块服务注册扩展方法
/// </summary>
internal static class OrdersServiceCollectionExtensions
{
    /// <summary>
    /// 注册订单模块的基础设施层服务（数据库上下文和仓储）
    /// </summary>
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ordersdb")
            ?? throw new InvalidOperationException("Connection string 'ordersdb' not found.");

        services.AddDbContext<OrdersDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(OrdersDbContext).Assembly.FullName);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }

    /// <summary>
    /// 注册订单模块的应用层服务（领域事件处理器和事件订阅者）
    /// </summary>
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        services.AddTransient<IDomainEventHandler<OrderCreatedDomainEvent>, OrderCreatedDomainEventHandler>();
        services.AddTransient<IDomainEventHandler<OrderPaidDomainEvent>, OrderPaidDomainEventHandler>();
        services.AddTransient<IDomainEventHandler<OrderCancelledDomainEvent>, OrderCancelledDomainEventHandler>();
        services.AddTransient<InventoryEventSubscriber>();
        services.AddTransient<PaymentEventSubscriber>();

        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("DotNetModulith.Modules.Orders"),
            SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        });

        return services;
    }
}
