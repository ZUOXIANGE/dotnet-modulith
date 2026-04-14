using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.ModulithCore;

/// <summary>
/// 模块接口，定义模块的元数据和服务注册能力
/// </summary>
public interface IModule
{
    /// <summary>
    /// 模块名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 模块基础命名空间
    /// </summary>
    string BaseNamespace { get; }

    /// <summary>
    /// 模块依赖列表
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// 模块发布的集成事件列表
    /// </summary>
    IReadOnlyList<string> PublishedEvents { get; }

    /// <summary>
    /// 模块订阅的集成事件列表
    /// </summary>
    IReadOnlyList<string> SubscribedEvents { get; }

    /// <summary>
    /// 注册模块所需的服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>注册了模块服务的服务集合</returns>
    IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration);
}
