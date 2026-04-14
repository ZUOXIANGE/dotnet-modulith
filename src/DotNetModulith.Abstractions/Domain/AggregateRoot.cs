namespace DotNetModulith.Abstractions.Domain;

/// <summary>
/// 聚合根基类，管理领域事件集合，支持事件的添加和清除
/// </summary>
public abstract class AggregateRoot : IAggregateRoot
{
    private readonly List<Events.IDomainEvent> _domainEvents = [];

    /// <summary>
    /// 未发布的领域事件列表
    /// </summary>
    public IReadOnlyList<Events.IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// 添加领域事件到事件集合
    /// </summary>
    /// <param name="domainEvent">要添加的领域事件</param>
    protected void AddDomainEvent(Events.IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// 清除所有已发布的领域事件
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
