using DotNetCore.CAP;
using DotNetModulith.Modules.Inventory;
using DotNetModulith.Modules.Inventory.Api;
using DotNetModulith.Modules.Notifications;
using DotNetModulith.Modules.Orders;
using DotNetModulith.Modules.Orders.Api;
using DotNetModulith.Modules.Payments;
using DotNetModulith.ModulithCore;
using Mediator;
using Microsoft.EntityFrameworkCore;
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
            path: "logs/log-.json",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true));
},
writeToProviders: true);

builder.AddServiceDefaults();

builder.Services.AddModulithCore();

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

builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.Namespace = "DotNetModulith.Mediator";
});

var isTesting = builder.Environment.IsEnvironment("Testing");

if (!isTesting)
{
    builder.Services.AddCap(capOptions =>
    {
        capOptions.DefaultGroupName = "modulith";
        capOptions.Version = "v1";
        capOptions.FailedRetryCount = 5;
        capOptions.FailedRetryInterval = 60;
        capOptions.FailedThresholdCallback = failed =>
        {
            var logger = failed.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError("CAP message failed after retries: {MessageType}",
                failed.MessageType);
        };

        var capDbConnection = builder.Configuration.GetConnectionString("modulithdb")
            ?? throw new InvalidOperationException("CAP database connection string not found.");

        capOptions.UsePostgreSql(capDbConnection);

        capOptions.UseRabbitMQ(rabbitOptions =>
        {
            rabbitOptions.HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
            rabbitOptions.UserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest";
            rabbitOptions.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
            rabbitOptions.VirtualHost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
            rabbitOptions.ExchangeName = "modulith.events";
        });

        capOptions.UseDashboard(dashboardOptions =>
        {
            dashboardOptions.PathMatch = "/cap-dashboard";
        });
    });
}

builder.Services.RegisterModule<InventoryModule>(builder.Configuration);
builder.Services.RegisterModule<OrdersModule>(builder.Configuration);
builder.Services.RegisterModule<PaymentsModule>(builder.Configuration);
builder.Services.RegisterModule<NotificationsModule>(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("modulithdb")!,
        name: "postgresql",
        tags: ["ready"]);

var app = builder.Build();

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

app.MapDefaultEndpoints();

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

app.UseHttpsRedirection();

app.MapOrderEndpoints();
app.MapInventoryEndpoints();

app.MapGet("/api/modules", (ModuleRegistry registry) =>
{
    var modules = registry.Modules.Select(m => new
    {
        m.Name,
        m.BaseNamespace,
        Dependencies = m.Dependencies,
        PublishedEvents = m.PublishedEvents,
        SubscribedEvents = m.SubscribedEvents
    });

    return Microsoft.AspNetCore.Http.Results.Ok(modules);
})
.WithName("GetModules")
.WithSummary("获取模块列表")
.WithDescription("获取系统中所有已注册模块的信息，包括名称、命名空间、依赖关系、发布事件和订阅事件")
.WithTags("模块管理");

app.MapGet("/api/modules/graph", (ModuleRegistry registry) =>
{
    var graph = registry.BuildDependencyGraph();
    return Microsoft.AspNetCore.Http.Results.Ok(new
    {
        Mermaid = graph.ToMermaid(),
        PlantUml = graph.ToPlantUml()
    });
})
.WithName("GetModuleGraph")
.WithSummary("获取模块依赖图")
.WithDescription("获取模块间依赖关系的可视化图表数据，支持 Mermaid 和 PlantUML 格式")
.WithTags("模块管理");

app.MapGet("/api/modules/verify", (ModuleBoundaryVerifier verifier) =>
{
    var violations = verifier.VerifyBoundaries();
    return violations.Count == 0
        ? Microsoft.AspNetCore.Http.Results.Ok(new { status = "healthy", violations = 0 })
        : Microsoft.AspNetCore.Http.Results.Ok(new { status = "violations_detected", violations });
})
.WithName("VerifyModuleBoundaries")
.WithSummary("验证模块边界")
.WithDescription("验证各模块是否遵守边界约束，检测是否存在违规的跨模块依赖")
.WithTags("模块管理");

app.Run();

public partial class Program;
