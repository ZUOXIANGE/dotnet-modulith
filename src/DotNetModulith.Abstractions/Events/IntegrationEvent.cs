using System.Text.Json.Serialization;

namespace DotNetModulith.Abstractions.Events;

/// <summary>
/// 集成事件基类，提供事件ID、发生时间和事件类型等公共属性
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
        EventType = GetType().Name;
    }

    /// <summary>
    /// 事件唯一标识
    /// </summary>
    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// 事件类型名称
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; init; }
}
