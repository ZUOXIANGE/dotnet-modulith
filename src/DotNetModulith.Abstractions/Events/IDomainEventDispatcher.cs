namespace DotNetModulith.Abstractions.Events;

/// <summary>
/// 领域事件分发器接口，负责将领域事件分发给对应的处理器
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// 分发领域事件集合
    /// </summary>
    /// <param name="domainEvents">领域事件集合</param>
    /// <param name="ct">取消令牌</param>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default);
}
