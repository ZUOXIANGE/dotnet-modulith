using DotNetModulith.JobHost;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("DotNetCore.CAP", LogEventLevel.Information)
        .MinimumLevel.Override("DotNetModulith", LogEventLevel.Debug)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("service", context.HostingEnvironment.ApplicationName)
        .WriteTo.Async(writeTo => writeTo.Console(new RenderedCompactJsonFormatter()))
        .WriteTo.Async(writeTo => writeTo.File(
            formatter: new RenderedCompactJsonFormatter(),
            path: "logs/log-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true));
},
writeToProviders: true);

builder.AddServiceDefaults();

builder.Services.AddJobHostServices(builder.Configuration);

var app = builder.Build();

app.UseJobHostEndpoints();

app.Run();

public partial class Program;
