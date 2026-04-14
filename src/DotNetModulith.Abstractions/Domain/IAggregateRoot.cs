namespace DotNetModulith.Abstractions.Domain;

/// <summary>
/// 聚合根接口，管理领域事件集合，支持事件溯源和发布
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// 未发布的领域事件列表
    /// </summary>
    IReadOnlyList<Events.IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// 清除所有已发布的领域事件
    /// </summary>
    void ClearDomainEvents();
}
