using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotNetModulith.Api.HealthChecks;

/// <summary>
/// 启动阶段健康检查
/// </summary>
internal sealed class StartupHealthCheck : IHealthCheck
{
    private readonly IHostApplicationLifetime _applicationLifetime;

    public StartupHealthCheck(IHostApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_applicationLifetime.ApplicationStarted.IsCancellationRequested)
            return Task.FromResult(HealthCheckResult.Unhealthy("Application is starting"));

        return Task.FromResult(HealthCheckResult.Healthy("Application startup completed"));
    }
}

/// <summary>
/// 滚动发布就绪状态健康检查
/// </summary>
internal sealed class ReadinessStateHealthCheck : IHealthCheck
{
    private readonly IHostApplicationLifetime _applicationLifetime;

    public ReadinessStateHealthCheck(IHostApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_applicationLifetime.ApplicationStarted.IsCancellationRequested)
            return Task.FromResult(HealthCheckResult.Unhealthy("Application is not started"));

        if (_applicationLifetime.ApplicationStopping.IsCancellationRequested)
            return Task.FromResult(HealthCheckResult.Unhealthy("Application is stopping"));

        return Task.FromResult(HealthCheckResult.Healthy("Application is ready"));
    }
}
