using DotNetModulith.Abstractions.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotNetModulith.Api.Controllers;

/// <summary>
/// 健康检查接口
/// </summary>
[ApiController]
[Route("")]
public sealed class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// 存活探针
    /// </summary>
    /// <returns>服务存活状态</returns>
    [HttpGet("alive")]
    public async Task<ApiResponse<object?>> Alive(CancellationToken ct)
    {
        return await ProbeAsync("alive", registration => registration.Tags.Contains("live"), ct);
    }

    /// <summary>
    /// 就绪探针
    /// </summary>
    /// <returns>服务就绪状态</returns>
    [HttpGet("ready")]
    public async Task<ApiResponse<object?>> Ready(CancellationToken ct)
    {
        return await ProbeAsync("ready", registration => registration.Tags.Contains("ready"), ct);
    }

    /// <summary>
    /// 启动探针
    /// </summary>
    /// <returns>服务启动状态</returns>
    [HttpGet("startup")]
    public async Task<ApiResponse<object?>> Startup(CancellationToken ct)
    {
        return await ProbeAsync("startup", registration => registration.Tags.Contains("startup"), ct);
    }

    /// <summary>
    /// 就绪探针别名
    /// </summary>
    /// <returns>服务就绪状态</returns>
    [HttpGet("health")]
    public async Task<ApiResponse<object?>> Health(CancellationToken ct)
    {
        return await Ready(ct);
    }

    private async Task<ApiResponse<object?>> ProbeAsync(
        string probeName,
        Func<HealthCheckRegistration, bool> predicate,
        CancellationToken ct)
    {
        var report = await _healthCheckService.CheckHealthAsync(predicate, ct);
        var isHealthy = report.Status == HealthStatus.Healthy;

        HttpContext.Response.StatusCode = isHealthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

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
            ? ApiResponse.Success<object?>(payload)
            : ApiResponse.Failure<object?>("unhealthy", ApiCodes.Common.InternalError, payload);
    }
}
