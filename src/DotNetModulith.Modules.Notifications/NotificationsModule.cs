using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Notifications;

public sealed class NotificationsModule : IModule
{
    public string Name => "Notifications";

    public string BaseNamespace => "DotNetModulith.Modules.Notifications";

    public IReadOnlyList<string> Dependencies => [];

    public IReadOnlyList<string> PublishedEvents => [];

    public IReadOnlyList<string> SubscribedEvents => [];

    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddNotificationsInfrastructure(configuration);
        services.AddNotificationsApplication(configuration);
        return services;
    }
}
