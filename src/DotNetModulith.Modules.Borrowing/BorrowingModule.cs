using DotNetModulith.Abstractions.Events;
using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Borrowing;

public sealed class BorrowingModule : IModule
{
    public string Name => "Borrowing";

    public string BaseNamespace => "DotNetModulith.Modules.Borrowing";

    public IReadOnlyList<string> Dependencies => ["Books", "Members"];

    public IReadOnlyList<string> PublishedEvents => [nameof(BookBorrowedIntegrationEvent), nameof(BookReturnedIntegrationEvent), nameof(BookOverdueIntegrationEvent)];

    public IReadOnlyList<string> SubscribedEvents => [];

    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddBorrowingInfrastructure(configuration);
        services.AddBorrowingApplication(configuration);
        return services;
    }
}
