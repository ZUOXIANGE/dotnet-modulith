using DotNetCore.CAP;

namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// CAP发布者的空实现，用于测试环境中替代RabbitMQ
/// </summary>
internal sealed class NullCapPublisher : ICapPublisher
{
    public IServiceProvider ServiceProvider { get; } = default!;

    public ICapTransaction? Transaction { get; set; }

    public Task PublishAsync<T>(string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishAsync<T>(string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void Publish<T>(string name, T? contentObj, string? callbackName = null) { }

    public void Publish<T>(string name, T? contentObj, IDictionary<string, string?> headers) { }

    public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void PublishDelay<T>(TimeSpan delayTime, string name, T? contentObj, IDictionary<string, string?> headers) { }

    public void PublishDelay<T>(TimeSpan delayTime, string name, T? contentObj, string? callbackName = null) { }
}
