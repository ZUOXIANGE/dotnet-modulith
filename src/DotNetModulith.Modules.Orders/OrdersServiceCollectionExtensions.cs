using System.Diagnostics;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Orders.Application.Caching;
using DotNetModulith.Modules.Orders.Application.Events;
using DotNetModulith.Modules.Orders.Application.Subscribers;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Domain.Events;
using DotNetModulith.Modules.Orders.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

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
        var connectionString = configuration.GetConnectionString("modulithdb")
            ?? throw new InvalidOperationException("Connection string 'modulithdb' not found.");

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
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OrdersCacheOptions>()
            .Bind(configuration.GetSection(OrdersCacheOptions.SectionName));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("redis") ?? "localhost:6379";
            options.InstanceName = "dotnet-modulith:";
        });

        services.AddFusionCache()
            .WithDefaultEntryOptions(options =>
            {
                options.Duration = TimeSpan.FromMinutes(5);
                options.MemoryCacheDuration = TimeSpan.FromSeconds(30);
                options.DistributedCacheDuration = TimeSpan.FromMinutes(5);
                options.IsFailSafeEnabled = true;
                options.FailSafeMaxDuration = TimeSpan.FromMinutes(30);
            })
            .WithSystemTextJsonSerializer()
            .TryWithRegisteredDistributedCache();

        services.AddSingleton<IConfigureOptions<FusionCacheOptions>, ConfigureOrdersFusionCacheOptions>();

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

internal sealed class ConfigureOrdersFusionCacheOptions : IConfigureOptions<FusionCacheOptions>
{
    private readonly IOptions<OrdersCacheOptions> _cacheOptions;

    public ConfigureOrdersFusionCacheOptions(IOptions<OrdersCacheOptions> cacheOptions)
    {
        _cacheOptions = cacheOptions;
    }

    public void Configure(FusionCacheOptions options)
    {
        var cache = _cacheOptions.Value;
        options.DefaultEntryOptions.Duration = cache.Duration;
        options.DefaultEntryOptions.MemoryCacheDuration = cache.MemoryCacheDuration;
        options.DefaultEntryOptions.DistributedCacheDuration = cache.DistributedCacheDuration;
        options.DefaultEntryOptions.IsFailSafeEnabled = cache.EnableFailSafe;
        options.DefaultEntryOptions.FailSafeMaxDuration = cache.FailSafeMaxDuration;
    }
}
