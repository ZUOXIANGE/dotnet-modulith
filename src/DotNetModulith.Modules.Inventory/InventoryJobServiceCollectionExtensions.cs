using DotNetModulith.Modules.Inventory.Application.Jobs;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Inventory;

public static class InventoryJobServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryJobServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("modulithdb")
            ?? throw new InvalidOperationException("Connection string 'modulithdb' not found.");

        services.AddDbContext<InventoryDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        services.AddOptions<LowStockAlertOptions>()
            .Bind(configuration.GetSection(LowStockAlertOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IStockRepository, StockRepository>();
        services.AddTransient<LowStockAlertJob>();

        return services;
    }
}
