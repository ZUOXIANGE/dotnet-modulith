namespace DotNetModulith.Abstractions.Events;

/// <summary>
/// 领域事件基类，提供事件ID和发生时间等公共属性
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 事件唯一标识
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }
}
