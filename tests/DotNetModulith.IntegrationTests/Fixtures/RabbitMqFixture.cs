using Testcontainers.RabbitMq;
using Xunit;

namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// RabbitMQ测试容器夹具，提供独立的RabbitMQ消息代理实例用于集成测试
/// </summary>
public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .WithCleanUp(true)
        .Build();

    /// <summary>
    /// RabbitMQ主机名
    /// </summary>
    public string HostName => _container.Hostname;

    /// <summary>
    /// RabbitMQ映射端口
    /// </summary>
    public int Port => _container.GetMappedPublicPort(5672);

    /// <summary>
    /// AMQP连接字符串
    /// </summary>
    public string ConnectionString => $"amqp://guest:guest@{HostName}:{Port}";

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
