using DotNetModulith.Modules.Reservation.Application;
using DotNetModulith.Modules.Reservation.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Reservation;

public static class ReservationServiceCollectionExtensions
{
    public static IServiceCollection AddReservationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("modulithdb")
            ?? throw new InvalidOperationException("Connection string 'modulithdb' not found.");

        services.AddDbContext<ReservationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ReservationDbContext).Assembly.FullName);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        return services;
    }

    public static IServiceCollection AddReservationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IReservationService, ReservationService>();

        return services;
    }
}
