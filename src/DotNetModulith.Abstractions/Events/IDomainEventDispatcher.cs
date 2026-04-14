using DotNetModulith.Abstractions.Domain;

namespace DotNetModulith.Abstractions.Events;

/// <summary>
/// 领域事件派发器接口，用于将聚合根中的领域事件发布到外部
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// 派发指定聚合根中的所有领域事件，派发完成后自动清除
    /// </summary>
    /// <param name="aggregateRoot">包含待派发领域事件的聚合根</param>
    /// <param name="ct">取消令牌</param>
    Task DispatchAsync(IAggregateRoot aggregateRoot, CancellationToken ct = default);
}
