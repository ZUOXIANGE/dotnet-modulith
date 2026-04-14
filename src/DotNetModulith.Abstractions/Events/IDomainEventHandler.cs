namespace DotNetModulith.Abstractions.Events;

/// <summary>
/// 领域事件处理器接口，用于处理特定类型的领域事件
/// </summary>
/// <typeparam name="TDomainEvent">领域事件类型</typeparam>
public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// 处理领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件实例</param>
    /// <param name="ct">取消令牌</param>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken ct = default);
}
