using DotNetModulith.Abstractions.Events;
using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Reports;

public sealed class ReportsModule : IModule
{
    public string Name => "Reports";

    public string BaseNamespace => "DotNetModulith.Modules.Reports";

    public IReadOnlyList<string> Dependencies => ["Books", "Borrowing", "Fines", "Members"];

    public IReadOnlyList<string> PublishedEvents => [];

    public IReadOnlyList<string> SubscribedEvents => [nameof(BookBorrowedIntegrationEvent), nameof(BookReturnedIntegrationEvent)];

    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddReportsApplication(configuration);
        return services;
    }
}
