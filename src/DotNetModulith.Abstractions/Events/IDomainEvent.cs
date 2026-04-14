namespace DotNetModulith.Abstractions.Events;

/// <summary>
/// 领域事件接口，用于聚合根内部的业务事件传播
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// 事件唯一标识
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
