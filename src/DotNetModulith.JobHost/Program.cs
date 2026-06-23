using DotNetModulith.JobHost;
using DotNetModulith.Modules.Books;
using DotNetModulith.Modules.Borrowing;
using DotNetModulith.Modules.Fines;
using DotNetModulith.Modules.Members;
using DotNetModulith.Modules.Notifications;
using DotNetModulith.Modules.Reservation;
using DotNetModulith.Modules.Storage;
using DotNetModulith.ModulithCore;
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
        .MinimumLevel.Override("System.Net.Http.HttpClient.OtlpLogExporter", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http.HttpClient.OtlpMetricExporter", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http.HttpClient.OtlpTraceExporter", LogEventLevel.Warning)
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

builder.Services.AddModulithCore(builder.Configuration);
builder.Services
    .RegisterModule<BooksModule>(builder.Configuration)
    .RegisterModule<MembersModule>(builder.Configuration)
    .RegisterModule<BorrowingModule>(builder.Configuration)
    .RegisterModule<ReservationModule>(builder.Configuration)
    .RegisterModule<FinesModule>(builder.Configuration)
    .RegisterModule<NotificationsModule>(builder.Configuration)
    .RegisterModule<StorageModule>(builder.Configuration);

var app = builder.Build();

app.UseJobHostEndpoints();

app.Run();

public partial class Program;
