using DotNetModulith.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetModulith.ModulithCore;

/// <summary>
/// 领域事件分发器实现，通过依赖注入查找并调用对应的领域事件处理器
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

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchSingleAsync(domainEvent, ct);
        }
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
