using DotNetModulith.Abstractions.Events;
using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Reservation;

public sealed class ReservationModule : IModule
{
    public string Name => "Reservation";

    public string BaseNamespace => "DotNetModulith.Modules.Reservation";

    public IReadOnlyList<string> Dependencies => ["Books", "Members"];

    public IReadOnlyList<string> PublishedEvents => [nameof(ReservationAvailableIntegrationEvent), nameof(ReservationExpiredIntegrationEvent)];

    public IReadOnlyList<string> SubscribedEvents => [];

    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddReservationInfrastructure(configuration);
        services.AddReservationApplication(configuration);
        return services;
    }
}
