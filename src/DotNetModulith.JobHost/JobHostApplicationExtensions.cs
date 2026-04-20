using DotNetModulith.Abstractions.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TickerQ.DependencyInjection;

namespace DotNetModulith.JobHost;

public static class JobHostApplicationExtensions
{
    public static WebApplication UseJobHostEndpoints(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionHandlerFeature?.Error;

                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsJsonAsync(
                    ApiResponse.Failure("internal server error", ApiCodes.Common.InternalError),
                    context.RequestAborted);

                if (exception is not null)
                {
                    app.Logger.LogError(exception, "Unhandled exception in JobHost");
                }
            });
        });

        app.UseTickerQ();

        app.MapGet("/", () => Results.Redirect("/tickerq-dashboard"));
        app.MapGet("/alive", (HealthCheckService healthChecks, CancellationToken ct) => ProbeAsync("alive", healthChecks, registration => registration.Tags.Contains("live"), ct));
        app.MapGet("/ready", (HealthCheckService healthChecks, CancellationToken ct) => ProbeAsync("ready", healthChecks, registration => registration.Tags.Contains("ready"), ct));
        app.MapGet("/startup", (HealthCheckService healthChecks, CancellationToken ct) => ProbeAsync("startup", healthChecks, registration => registration.Tags.Contains("startup"), ct));
        app.MapGet("/health", (HealthCheckService healthChecks, CancellationToken ct) => ProbeAsync("health", healthChecks, registration => registration.Tags.Contains("ready"), ct));

        return app;
    }

    private static async Task<IResult> ProbeAsync(
        string probeName,
        HealthCheckService healthCheckService,
        Func<HealthCheckRegistration, bool> predicate,
        CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(predicate, cancellationToken);
        var isHealthy = report.Status == HealthStatus.Healthy;

        object payload = new
        {
            probe = probeName,
            status = report.Status.ToString().ToLowerInvariant(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                x => x.Key,
                x => new
                {
                    status = x.Value.Status.ToString().ToLowerInvariant(),
                    description = x.Value.Description
                })
        };

        return isHealthy
            ? Results.Json(ApiResponse.Success<object?>(payload), statusCode: StatusCodes.Status200OK)
            : Results.Json(ApiResponse.Failure<object?>("unhealthy", ApiCodes.Common.InternalError, payload), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
