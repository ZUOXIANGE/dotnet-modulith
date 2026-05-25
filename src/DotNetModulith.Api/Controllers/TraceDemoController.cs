using System.Diagnostics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Authorization;
using DotNetModulith.Abstractions.Contracts.TraceDemo;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Api.Controllers;

/// <summary>
/// 链路追踪演示接口，在一个请求中串联数据库写入、缓存读写、队列发送和消息处理
/// </summary>
[ApiController]
[Route("api/trace-demo")]
[Authorize(Policy = PermissionCodes.OrdersManage)]
public sealed class TraceDemoController : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Api");

    private readonly OrdersDbContext _dbContext;
    private readonly IFusionCache _cache;
    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<TraceDemoController> _logger;

    public TraceDemoController(
        OrdersDbContext dbContext,
        IFusionCache cache,
        ICapPublisher capPublisher,
        ILogger<TraceDemoController> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _capPublisher = capPublisher;
        _logger = logger;
    }

    /// <summary>
    /// 执行链路追踪演示，完整展示 API → DB → Cache → Queue → Subscriber 的追踪链路
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>演示结果，包含演示ID和各步骤执行信息</returns>
    [HttpPost]
    public async Task<ApiResponse<TraceDemoResponse>> RunTraceDemo(CancellationToken ct)
    {
        var demoId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;

        using var rootActivity = ActivitySource.StartActivity("TraceDemo.RunFullChain", ActivityKind.Server);
        rootActivity?.SetTag("modulith.demo_id", demoId);
        rootActivity?.SetTag("modulith.started_at", startedAt.ToString("O"));

        var steps = new List<TraceDemoStep>();

        try
        {
            // Step 1: Database Write
            steps.Add(await ExecuteDbWriteStep(demoId, ct));

            // Step 2: Cache Write
            steps.Add(await ExecuteCacheWriteStep(demoId, ct));

            // Step 3: Cache Read
            steps.Add(await ExecuteCacheReadStep(demoId, ct));

            // Step 4: Publish Integration Event
            steps.Add(await ExecutePublishStep(demoId, ct));

            rootActivity?.SetStatus(ActivityStatusCode.Ok);
            rootActivity?.SetTag("modulith.steps_count", steps.Count);

            _logger.LogInformation(
                "Trace demo {DemoId} completed with {StepCount} steps in {ElapsedMs}ms",
                demoId, steps.Count, (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);

            return ApiResponse.Success(new TraceDemoResponse(
                demoId,
                startedAt,
                DateTimeOffset.UtcNow,
                steps));
        }
        catch (Exception ex)
        {
            rootActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            rootActivity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                ["exception.type"] = ex.GetType().FullName,
                ["exception.message"] = ex.Message
            }));

            _logger.LogError(ex, "Trace demo {DemoId} failed", demoId);

            throw;
        }
    }

    private async Task<TraceDemoStep> ExecuteDbWriteStep(string demoId, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var activity = ActivitySource.StartActivity("TraceDemo.DatabaseWrite", ActivityKind.Internal);
        activity?.SetTag("modulith.demo_id", demoId);
        activity?.SetTag("modulith.operation", "create_order");

        var orderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var order = new OrderEntity
        {
            Id = orderId,
            CustomerId = $"__trace_demo_{demoId}",
            Status = OrderStatus.Pending,
            TotalAmount = 0m,
            CreatedAt = now,
            Lines =
            [
                new OrderLineEntity("TRACE-DEMO", "Trace Demo Item", 1, 0m)
            ]
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        sw.Stop();

        activity?.SetTag("modulith.order_id", orderId.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);

        _logger.LogDebug("TraceDemo: DB write completed for order {OrderId} in {ElapsedMs}ms",
            orderId, sw.ElapsedMilliseconds);

        return new TraceDemoStep(
            "DatabaseWrite",
            "success",
            sw.ElapsedMilliseconds,
            new Dictionary<string, string>
            {
                ["entity"] = nameof(OrderEntity),
                ["schema"] = "orders",
                ["table"] = "orders",
                ["orderId"] = orderId.ToString()
            });
    }

    private async Task<TraceDemoStep> ExecuteCacheWriteStep(string demoId, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var activity = ActivitySource.StartActivity("TraceDemo.CacheWrite", ActivityKind.Internal);
        activity?.SetTag("modulith.demo_id", demoId);
        activity?.SetTag("modulith.operation", "cache_set");

        var cacheKey = $"trace_demo:{demoId}";
        var cacheValue = new TraceDemoCacheValue(demoId, DateTimeOffset.UtcNow, "written from trace demo");

        await _cache.SetAsync<TraceDemoCacheValue>(cacheKey, cacheValue, (FusionCacheEntryOptions?)null, ct);

        sw.Stop();

        activity?.SetTag("modulith.cache_key", cacheKey);
        activity?.SetTag("modulith.cache_layer", "FusionCache(L1+L2)");
        activity?.SetStatus(ActivityStatusCode.Ok);

        _logger.LogDebug("TraceDemo: Cache write completed for key {CacheKey} in {ElapsedMs}ms",
            cacheKey, sw.ElapsedMilliseconds);

        return new TraceDemoStep(
            "CacheWrite",
            "success",
            sw.ElapsedMilliseconds,
            new Dictionary<string, string>
            {
                ["cacheKey"] = cacheKey,
                ["cacheLayer"] = "FusionCache(L1+L2)"
            });
    }

    private async Task<TraceDemoStep> ExecuteCacheReadStep(string demoId, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var activity = ActivitySource.StartActivity("TraceDemo.CacheRead", ActivityKind.Internal);
        activity?.SetTag("modulith.demo_id", demoId);
        activity?.SetTag("modulith.operation", "cache_get");

        var cacheKey = $"trace_demo:{demoId}";

        var cached = await _cache.GetOrSetAsync<TraceDemoCacheValue?>(
            cacheKey,
            async (_, _) =>
            {
                // This simulates a cache miss fallback (should not happen since we just wrote it)
                return new TraceDemoCacheValue(demoId, DateTimeOffset.UtcNow, "cache miss fallback");
            },
            null,
            null,
            null,
            ct);

        sw.Stop();

        activity?.SetTag("modulith.cache_key", cacheKey);
        activity?.SetTag("modulith.cache_hit", cached is not null);
        activity?.SetStatus(ActivityStatusCode.Ok);

        _logger.LogDebug("TraceDemo: Cache read completed for key {CacheKey} in {ElapsedMs}ms, hit={Hit}",
            cacheKey, sw.ElapsedMilliseconds, cached is not null);

        return new TraceDemoStep(
            "CacheRead",
            "success",
            sw.ElapsedMilliseconds,
            new Dictionary<string, string>
            {
                ["cacheKey"] = cacheKey,
                ["cacheHit"] = (cached is not null).ToString()
            });
    }

    private async Task<TraceDemoStep> ExecutePublishStep(string demoId, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var activity = ActivitySource.StartActivity("TraceDemo.PublishEvent", ActivityKind.Producer);
        activity?.SetTag("modulith.demo_id", demoId);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination_kind", "topic");

        var integrationEvent = new TraceDemoIntegrationEvent(
            demoId,
            "Trace demo running across API -> DB -> Cache -> Queue -> Subscriber",
            DateTimeOffset.UtcNow);

        var topic = $"modulith.trace.{integrationEvent.EventType}";

        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("modulith.event_type", integrationEvent.EventType);
        activity?.SetTag("modulith.event_id", integrationEvent.EventId.ToString());

        await _capPublisher.PublishAsync(topic, integrationEvent, cancellationToken: ct);

        sw.Stop();

        activity?.SetStatus(ActivityStatusCode.Ok);

        _logger.LogInformation(
            "TraceDemo: Published event {EventType} to topic {Topic} with EventId {EventId} in {ElapsedMs}ms",
            integrationEvent.EventType, topic, integrationEvent.EventId, sw.ElapsedMilliseconds);

        return new TraceDemoStep(
            "EventPublish",
            "success",
            sw.ElapsedMilliseconds,
            new Dictionary<string, string>
            {
                ["topic"] = topic,
                ["eventType"] = integrationEvent.EventType,
                ["eventId"] = integrationEvent.EventId.ToString(),
                ["messagingSystem"] = "RabbitMQ"
            });
    }
}

/// <summary>
/// 链路追踪演示响应
/// </summary>
/// <param name="DemoId">演示会话ID</param>
/// <param name="StartedAt">演示开始时间</param>
/// <param name="CompletedAt">演示完成时间</param>
/// <param name="Steps">各步骤执行详情</param>
public sealed record TraceDemoResponse(
    string DemoId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    IReadOnlyList<TraceDemoStep> Steps);

/// <summary>
/// 链路追踪演示步骤详情
/// </summary>
/// <param name="Name">步骤名称</param>
/// <param name="Status">执行状态</param>
/// <param name="DurationMs">耗时（毫秒）</param>
/// <param name="Details">步骤详情元数据</param>
public sealed record TraceDemoStep(
    string Name,
    string Status,
    long DurationMs,
    IReadOnlyDictionary<string, string> Details);

/// <summary>
/// 链路追踪演示缓存值
/// </summary>
internal sealed record TraceDemoCacheValue(string DemoId, DateTimeOffset Timestamp, string Message);
