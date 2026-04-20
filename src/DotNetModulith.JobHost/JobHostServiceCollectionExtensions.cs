using DotNetCore.CAP;
using DotNetModulith.JobHost.Configuration;
using DotNetModulith.JobHost.Infrastructure;
using DotNetModulith.Modules.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace DotNetModulith.JobHost;

public static class JobHostServiceCollectionExtensions
{
    public static IServiceCollection AddJobHostServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<CapMessagingOptions>()
            .Bind(configuration.GetSection(CapMessagingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddInventoryJobServices(configuration);

        services.AddTickerQ(options =>
        {
            var tickerQConnection = configuration.GetConnectionString("tickerqdb")
                ?? throw new InvalidOperationException("TickerQ database connection string not found.");

            options.AddOperationalStore(ef =>
            {
                ef.SetSchema("ticker");
                ef.UseTickerQDbContext<TickerQSchedulerDbContext>(
                    db => db.UseNpgsql(
                        tickerQConnection,
                        npgsql => npgsql.MigrationsAssembly(typeof(TickerQSchedulerDbContext).Assembly.FullName)),
                    schema: "ticker");
            });

            options.AddDashboard(dashboard =>
            {
                dashboard.SetBasePath("/tickerq-dashboard");
                dashboard.SetGroupName("tickerq");
            });
        });

        var capSettings = configuration
            .GetSection(CapMessagingOptions.SectionName)
            .Get<CapMessagingOptions>() ?? new CapMessagingOptions();

        var rabbitMqOptions = configuration
            .GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddCap(cap =>
        {
            cap.DefaultGroupName = capSettings.DefaultGroupName;
            cap.Version = capSettings.Version;
            cap.FailedRetryCount = capSettings.FailedRetryCount;
            cap.FailedRetryInterval = capSettings.FailedRetryInterval;

            var capDbConnection = configuration.GetConnectionString("modulithdb")
                ?? throw new InvalidOperationException("CAP database connection string not found.");

            cap.UsePostgreSql(capDbConnection);

            cap.UseRabbitMQ(rabbitMq =>
            {
                rabbitMq.HostName = rabbitMqOptions.HostName;
                rabbitMq.Port = rabbitMqOptions.Port;
                rabbitMq.UserName = rabbitMqOptions.UserName;
                rabbitMq.Password = rabbitMqOptions.Password;
                rabbitMq.VirtualHost = rabbitMqOptions.VirtualHost;
            });
        });

        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("modulithdb")!,
                name: "postgresql",
                tags: ["ready"])
            .AddNpgSql(
                configuration.GetConnectionString("tickerqdb")!,
                name: "tickerq-postgresql",
                tags: ["ready"]);

        return services;
    }
}
