using DotNetModulith.Abstractions.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.ModulithCore;

/// <summary>
/// 模块核心服务注册扩展方法
/// </summary>
public static class ModuleServiceCollectionExtensions
{
    /// <summary>
    /// 注册模块化核心服务，包括模块注册表、边界验证器和领域事件派发器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>注册了核心服务的服务集合</returns>
    public static IServiceCollection AddModulithCore(this IServiceCollection services)
    {
        services.AddSingleton<ModuleRegistry>();
        services.AddSingleton<ModuleBoundaryVerifier>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }

    /// <summary>
    /// 注册指定模块，包括模块描述符和模块服务
    /// </summary>
    /// <typeparam name="TModule">模块类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>注册了模块服务的服务集合</returns>
    public static IServiceCollection RegisterModule<TModule>(this IServiceCollection services, IConfiguration configuration)
        where TModule : IModule, new()
    {
        var module = new TModule();

        var descriptor = new ModuleDescriptor(
            module.Name,
            module.BaseNamespace,
            typeof(TModule).Assembly,
            module.Dependencies,
            publishedEvents: module.PublishedEvents,
            subscribedEvents: module.SubscribedEvents);

        services.AddSingleton(descriptor);
        services.AddSingleton<IModuleDescriptor>(descriptor);

        module.AddModuleServices(services, configuration);

        return services;
    }
}
