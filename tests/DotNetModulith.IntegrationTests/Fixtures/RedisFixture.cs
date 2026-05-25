using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// Redis 测试容器夹具，为分布式缓存相关集成测试提供真实 Redis。
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder("redis:7-alpine")
        .WithPortBinding(6379, true)
        .WithCleanUp(true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(6379))
        .Build();

    public string ConnectionString => $"localhost:{_container.GetMappedPublicPort(6379)}";

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
