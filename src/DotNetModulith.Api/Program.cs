using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Api.Configuration;
using DotNetModulith.Api.HealthChecks;
using DotNetModulith.Modules.Inventory;
using DotNetModulith.Modules.Inventory.Api.Controllers;
using DotNetModulith.Modules.Notifications;
using DotNetModulith.Modules.Orders;
using DotNetModulith.Modules.Orders.Api.Controllers;
using DotNetModulith.Modules.Payments;
using DotNetModulith.ModulithCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
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

builder.Services.AddModulithCore(builder.Configuration);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "DotNetModulith API",
            Version = "v1",
            Description = "基于 ASP.NET Core 10 的模块化单体架构 API，借鉴 Spring Modulith 设计思想"
        };
        return Task.CompletedTask;
    });
});

builder.Services.AddProblemDetails();
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(OrdersController).Assembly)
    .AddApplicationPart(typeof(InventoryController).Assembly)
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "validation failed" : e.ErrorMessage)
                        .ToArray());

            return new OkObjectResult(ApiResponse.Failure(
                msg: "validation failed",
                code: ApiCodes.Common.ValidationFailed,
                data: new { errors }));
        };
    });

builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.Namespace = "DotNetModulith.Mediator";
});

builder.Services
    .AddOptions<CapMessagingOptions>()
    .Bind(builder.Configuration.GetSection(CapMessagingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var isTesting = builder.Environment.IsEnvironment("Testing");

if (isTesting && string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("modulithdb")))
{
    // 测试主机在配置注入时序异常时，提供兜底连接串，避免模块注册阶段直接失败。
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:modulithdb"] = "Host=localhost;Port=5432;Database=modulith;Username=postgres;Password=postgres"
    });
}

builder.Services.RegisterModule<InventoryModule>(builder.Configuration);
builder.Services.RegisterModule<OrdersModule>(builder.Configuration);
builder.Services.RegisterModule<PaymentsModule>(builder.Configuration);
builder.Services.RegisterModule<NotificationsModule>(builder.Configuration);

if (!isTesting)
{
    var capSettings = builder.Configuration
        .GetSection(CapMessagingOptions.SectionName)
        .Get<CapMessagingOptions>() ?? new CapMessagingOptions();

    var rabbitMqOptions = builder.Configuration
        .GetSection(RabbitMqOptions.SectionName)
        .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

    builder.Services.AddCap(cap =>
    {
        cap.DefaultGroupName = capSettings.DefaultGroupName;
        cap.Version = capSettings.Version;
        cap.FailedRetryCount = capSettings.FailedRetryCount;
        cap.FailedRetryInterval = capSettings.FailedRetryInterval;
        cap.FailedThresholdCallback = failed =>
        {
            var logger = failed.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError("CAP message failed after retries: {MessageType}",
                failed.MessageType);
        };

        var capDbConnection = builder.Configuration.GetConnectionString("modulithdb")
            ?? throw new InvalidOperationException("CAP database connection string not found.");

        cap.UsePostgreSql(capDbConnection);

        cap.UseRabbitMQ(rabbitOptions =>
        {
            rabbitOptions.HostName = rabbitMqOptions.HostName;
            rabbitOptions.Port = rabbitMqOptions.Port;
            rabbitOptions.UserName = rabbitMqOptions.UserName;
            rabbitOptions.Password = rabbitMqOptions.Password;
            rabbitOptions.VirtualHost = rabbitMqOptions.VirtualHost;
            rabbitOptions.ExchangeName = "modulith.events";
        });

        cap.UseDashboard(dashboardOptions =>
        {
            dashboardOptions.PathMatch = "/cap-dashboard";
        });
    });
}

var redisConnection = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
var (redisHost, redisPort) = TcpHealthCheck.ParseEndpoint(redisConnection, 6379);

var rabbitHost = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
var rabbitPort = builder.Configuration.GetValue<int?>("RabbitMQ:Port") ?? 5672;

builder.Services.AddHealthChecks()
    .AddCheck<StartupHealthCheck>("startup-state", tags: ["startup"])
    .AddCheck<ReadinessStateHealthCheck>("readiness-state", tags: ["ready"])
    .AddNpgSql(
        builder.Configuration.GetConnectionString("modulithdb")!,
        name: "postgresql",
        tags: ["ready"])
    .AddCheck("redis", new TcpHealthCheck(redisHost, redisPort), tags: ["ready"])
    .AddCheck("rabbitmq", new TcpHealthCheck(rabbitHost, rabbitPort), tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is null)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Failure("internal server error", ApiCodes.Common.InternalError),
                context.RequestAborted);
            return;
        }

        if (exception is BusinessException businessException)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Failure(
                    businessException.Message,
                    businessException.Code,
                    businessException.Payload),
                context.RequestAborted);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(
            ApiResponse.Failure("internal server error", ApiCodes.Common.InternalError),
            context.RequestAborted);
    });
});

app.UseWhen(
    context =>
        !context.Request.Path.StartsWithSegments("/health") &&
        !context.Request.Path.StartsWithSegments("/alive"),
    appBuilder => appBuilder.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, _, ex) => ex is null
            ? LogEventLevel.Information
            : LogEventLevel.Error;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
    }));

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
        .WithTitle("DotNetModulith API")
        .WithOpenApiRoutePattern("/openapi/{documentName}.json")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.Run();

public partial class Program;
