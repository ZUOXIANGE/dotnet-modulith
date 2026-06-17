using DotNetModulith.Abstractions.Events;
using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Fines;

public sealed class FinesModule : IModule
{
    public string Name => "Fines";

    public string BaseNamespace => "DotNetModulith.Modules.Fines";

    public IReadOnlyList<string> Dependencies => ["Members"];

    public IReadOnlyList<string> PublishedEvents => [nameof(FineCreatedIntegrationEvent), nameof(FinePaidIntegrationEvent)];

    public IReadOnlyList<string> SubscribedEvents => [nameof(BookOverdueIntegrationEvent)];

    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddFinesInfrastructure(configuration);
        services.AddFinesApplication(configuration);
        return services;
    }
}
