using DotNetModulith.Abstractions.Domain;
using DotNetModulith.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetModulith.ModulithCore;

/// <summary>
/// 领域事件派发器，将聚合根中的领域事件派发到已注册的IDomainEventHandler处理器
/// 派发完成后自动清除聚合根中的领域事件
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;
    private readonly DomainEventDispatcherOptions _options;

    public DomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<DomainEventDispatcher> logger,
        IOptions<DomainEventDispatcherOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 派发指定聚合根中的所有领域事件，派发完成后自动清除
    /// </summary>
    public async Task DispatchAsync(IAggregateRoot aggregateRoot, CancellationToken ct = default)
    {
        var events = aggregateRoot.DomainEvents.ToList();

        if (events.Count == 0)
            return;

        foreach (var domainEvent in events)
        {
            await DispatchSingleAsync(domainEvent, ct);
        }

        aggregateRoot.ClearDomainEvents();
    }

    private async Task DispatchSingleAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod is null)
                continue;

            try
            {
                var task = (Task?)handleMethod.Invoke(handler, [domainEvent, ct]);
                if (task is not null)
                {
                    await task;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling domain event {EventType}",
                    eventType.Name);

                if (!_options.ContinueOnHandlerError)
                {
                    throw;
                }
            }
        }
    }
}
