using DotNetModulith.Modules.Reports.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Reports;

public static class ReportsServiceCollectionExtensions
{
    public static IServiceCollection AddReportsApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
