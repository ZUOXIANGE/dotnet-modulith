using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Storage;

/// <summary>
/// 文件存储模块定义
/// </summary>
public sealed class StorageModule : IModule
{
    public string Name => "Storage";

    public string BaseNamespace => "DotNetModulith.Modules.Storage";

    public IReadOnlyList<string> Dependencies => [];

    public IReadOnlyList<string> PublishedEvents => [];

    public IReadOnlyList<string> SubscribedEvents => [];

    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddStorageServices(configuration);
        return services;
    }
}
