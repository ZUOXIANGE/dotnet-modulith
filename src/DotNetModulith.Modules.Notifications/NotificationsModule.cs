using DotNetModulith.Abstractions.Events;
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

    public IReadOnlyList<string> SubscribedEvents =>
    [
        nameof(BookBorrowedIntegrationEvent),
        nameof(BookReturnedIntegrationEvent),
        nameof(BookOverdueIntegrationEvent),
        nameof(ReservationAvailableIntegrationEvent),
        nameof(ReservationExpiredIntegrationEvent),
        nameof(FineCreatedIntegrationEvent),
        nameof(FinePaidIntegrationEvent)
    ];

    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddNotificationsInfrastructure(configuration);
        services.AddNotificationsApplication(configuration);
        return services;
    }
}
