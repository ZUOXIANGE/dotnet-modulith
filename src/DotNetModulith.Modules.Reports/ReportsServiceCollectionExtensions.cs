using DotNetModulith.Modules.Reports.Application;
using DotNetModulith.Modules.Reports.Application.Subscribers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Reports;

public static class ReportsServiceCollectionExtensions
{
    public static IServiceCollection AddReportsApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<BorrowingEventSubscriber>();
        services.AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(30),
                FailSafeMaxDuration = TimeSpan.FromHours(2),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30)
            });

        return services;
    }
}
