using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Users;

/// <summary>
/// 用户模块定义
/// </summary>
public sealed class UsersModule : IModule
{
    public string Name => "Users";

    public string BaseNamespace => "DotNetModulith.Modules.Users";

    public IReadOnlyList<string> Dependencies => [];

    public IReadOnlyList<string> PublishedEvents => [];

    public IReadOnlyList<string> SubscribedEvents => [];

    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddUsersInfrastructure(configuration);
        services.AddUsersApplication(configuration);
        return services;
    }
}
