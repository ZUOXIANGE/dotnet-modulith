using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// 服务默认配置扩展方法，提供OpenTelemetry、健康检查、服务发现和HTTP弹性能力的统一配置
/// </summary>
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>
    /// 添加服务默认配置，包括OpenTelemetry、健康检查、服务发现和HTTP弹性处理
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// 配置OpenTelemetry可观测性，包括日志、指标和链路追踪
    /// </summary>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var openObserveConfig = builder.Configuration.GetSection("OpenObserve");
        var openObserveEnabled = openObserveConfig.GetValue<bool?>("Enabled") ?? true;

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: builder.Environment.ApplicationName,
                serviceVersion: typeof(Extensions).Assembly.GetName().Version?.ToString() ?? "1.0.0")
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.SetResourceBuilder(resourceBuilder);

            if (openObserveEnabled)
            {
                logging.AddOtlpExporter(ConfigureOtlpForOpenObserve(
                    openObserveConfig, "logs", builder));
            }
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("DotNetModulith")
                    .AddMeter("DotNetCore.CAP");

                if (openObserveEnabled)
                {
                    metrics.AddOtlpExporter(ConfigureOtlpForOpenObserve(
                        openObserveConfig, "metrics", builder));
                }

                metrics.AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(builder.Environment.ApplicationName)
                    .AddSource("DotNetCore.CAP")
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath) &&
                            !context.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                        options.SetDbStatementForText = true)
                    .AddSource("DotNetModulith.Modules.Orders")
                    .AddSource("DotNetModulith.Modules.Inventory")
                    .AddSource("DotNetModulith.Modules.Payments")
                    .AddSource("DotNetModulith.Modules.Notifications");

                if (openObserveEnabled)
                {
                    tracing.AddOtlpExporter(ConfigureOtlpForOpenObserve(
                        openObserveConfig, "traces", builder));
                }
            });

        return builder;
    }

    /// <summary>
    /// 配置OpenObserve OTLP导出器选项
    /// </summary>
    private static Action<OtlpExporterOptions> ConfigureOtlpForOpenObserve(
        IConfigurationSection config, string signal, IHostApplicationBuilder builder)
    {
        return options =>
        {
            var endpoint = config.GetValue<string>("Endpoint")
                ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                ?? "http://localhost:5080";

            var organization = config.GetValue<string>("Organization") ?? "default";
            var streamName = config.GetValue<string>("StreamName")
                ?? builder.Environment.ApplicationName.ToLowerInvariant();

            options.Endpoint = new Uri($"{endpoint}/api/{organization}/v1/{signal}");
            options.Protocol = OtlpExportProtocol.HttpProtobuf;

            var email = config.GetValue<string>("UserEmail") ?? "admin@modulith.local";
            var password = config.GetValue<string>("UserPassword") ?? "Modulith@2026";
            var credentials = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{email}:{password}"));

            options.Headers = $"Authorization=Basic {credentials},stream-name={streamName}";
        };
    }

    /// <summary>
    /// 添加默认健康检查
    /// </summary>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// 映射默认端点，包括健康检查和Prometheus指标抓取端点
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(HealthEndpointPath);

        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        app.MapPrometheusScrapingEndpoint();

        return app;
    }
}
