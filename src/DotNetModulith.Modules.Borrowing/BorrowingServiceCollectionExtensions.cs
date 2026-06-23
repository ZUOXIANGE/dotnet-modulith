using DotNetModulith.Modules.Borrowing.Application;
using DotNetModulith.Modules.Borrowing.Application.Jobs;
using DotNetModulith.Modules.Borrowing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Borrowing;

public static class BorrowingServiceCollectionExtensions
{
    public static IServiceCollection AddBorrowingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("modulithdb")
            ?? throw new InvalidOperationException("Connection string 'modulithdb' not found.");

        services.AddDbContext<BorrowingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(BorrowingDbContext).Assembly.FullName);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        return services;
    }

    public static IServiceCollection AddBorrowingApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IBorrowingService, BorrowingService>();
        services.AddScoped<OverdueDetectionJob>();
        services.Configure<OverdueDetectionOptions>(configuration.GetSection(OverdueDetectionOptions.SectionName));

        return services;
    }
}
