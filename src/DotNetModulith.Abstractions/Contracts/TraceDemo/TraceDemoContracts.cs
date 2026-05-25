using System.Text.Json.Serialization;
using DotNetModulith.Abstractions.Events;

namespace DotNetModulith.Abstractions.Contracts.TraceDemo;

/// <summary>
/// 链路追踪演示集成事件，用于验证完整的事件驱动链路追踪
/// </summary>
/// <param name="DemoId">演示会话ID</param>
/// <param name="Message">演示消息</param>
/// <param name="Timestamp">事件发生时间</param>
public sealed record TraceDemoIntegrationEvent(
    [property: JsonPropertyName("demoId")] string DemoId,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp) : IntegrationEvent;
