namespace DotNetModulith.Abstractions.Events;

/// <summary>
/// 集成事件接口，用于跨模块的异步通信
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// 事件唯一标识
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// 事件类型名称
    /// </summary>
    string EventType { get; }
}
