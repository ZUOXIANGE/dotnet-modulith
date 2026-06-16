using DotNetModulith.Modules.Fines.Application;
using DotNetModulith.Modules.Fines.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Fines;

public static class FinesServiceCollectionExtensions
{
    public static IServiceCollection AddFinesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("modulithdb")
            ?? throw new InvalidOperationException("Connection string 'modulithdb' not found.");

        services.AddDbContext<FinesDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(FinesDbContext).Assembly.FullName);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        return services;
    }

    public static IServiceCollection AddFinesApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFineService, FineService>();

        return services;
    }
}
