using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Members;

public sealed class MembersModule : IModule
{
    public string Name => "Members";

    public string BaseNamespace => "DotNetModulith.Modules.Members";

    public IReadOnlyList<string> Dependencies => [];

    public IReadOnlyList<string> PublishedEvents => [];

    public IReadOnlyList<string> SubscribedEvents => [];

    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMembersInfrastructure(configuration);
        services.AddMembersApplication(configuration);
        return services;
    }
}
