using DotNetModulith.ModulithCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Books;

public sealed class BooksModule : IModule
{
    public string Name => "Books";

    public string BaseNamespace => "DotNetModulith.Modules.Books";

    public IReadOnlyList<string> Dependencies => [];

    public IReadOnlyList<string> PublishedEvents => [];

    public IReadOnlyList<string> SubscribedEvents => [];

    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddBooksInfrastructure(configuration);
        services.AddBooksApplication(configuration);
        return services;
    }
}
