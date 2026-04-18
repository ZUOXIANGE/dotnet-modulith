using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// 统一的服务默认配置扩展：
/// 包含 OpenTelemetry、健康检查、服务发现与默认 HTTP 客户端策略。
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 健康检查端点路径（就绪探针）。
    /// </summary>
    private const string HealthEndpointPath = "/health";
    /// <summary>
    /// 存活检查端点路径（存活探针）。
    /// </summary>
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>
    /// 为应用添加统一默认能力：
    /// OpenTelemetry、健康检查、服务发现与默认 HTTP 客户端配置。
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        // 统一 API 请求日志：记录请求方法/路径、响应状态码与耗时。
        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.Duration;
            options.CombineLogs = true;
        });

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// 配置 OpenTelemetry 日志、指标与链路追踪。
    /// 所有信号均通过 OTLP 上报（OpenObserve 或 OTEL Collector）。
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
                // 统一打上部署环境维度，便于按环境筛选观测数据。
                ["deployment.environment"] = builder.Environment.EnvironmentName
            });

        // 日志信号：通过 OTLP 输出到 OpenObserve 或 Collector。
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.SetResourceBuilder(resourceBuilder);

            if (openObserveEnabled)
            {
                logging.AddOtlpExporter(ConfigureOtlpForOpenObserve(
                    openObserveConfig, "logs", builder));
            }
            else
            {
                logging.AddOtlpExporter(ConfigureOtlpForCollector());
            }
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                // 指标信号：接入 ASP.NET Core、HttpClient、Runtime、Npgsql、FusionCache 等 instrumentation。
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddNpgsqlInstrumentation()
                    .AddFusionCacheInstrumentation()
                    .AddMeter("DotNetModulith")
                    .AddMeter("DotNetCore.CAP");

                if (openObserveEnabled)
                {
                    metrics.AddOtlpExporter(ConfigureOtlpForOpenObserve(
                        openObserveConfig, "metrics", builder));
                }
                else
                {
                    metrics.AddOtlpExporter(ConfigureOtlpForCollector());
                }
            })
            .WithTracing(tracing =>
            {
                // 链路信号：接入 Web 请求、HttpClient、Npgsql、EF Core 及各业务模块 ActivitySource。
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(builder.Environment.ApplicationName)
                    .AddSource("DotNetCore.CAP")
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath) &&
                            !context.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                    .AddHttpClientInstrumentation()
                    .AddNpgsql()
                    .AddFusionCacheInstrumentation()
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
                else
                {
                    tracing.AddOtlpExporter(ConfigureOtlpForCollector());
                }
            });

        return builder;
    }

    /// <summary>
    /// OTLP 导出配置（面向本地/通用 Collector）。
    /// </summary>
    private static Action<OtlpExporterOptions> ConfigureOtlpForCollector()
    {
        return options =>
        {
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
        };
    }

    /// <summary>
    /// OTLP 导出配置（面向 OpenObserve）。
    /// 自动拼接不同信号（logs/metrics/traces）的上报路径。
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

            // OpenObserve 以 signal 区分日志/指标/链路入口。
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
    /// 注册默认健康检查项。
    /// </summary>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// 映射默认基础端点（健康检查与存活检查）。
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(HealthEndpointPath);

        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
