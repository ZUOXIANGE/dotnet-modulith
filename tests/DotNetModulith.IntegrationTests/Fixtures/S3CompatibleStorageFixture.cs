using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// S3 兼容对象存储测试容器夹具
/// </summary>
public sealed class S3CompatibleStorageFixture : IAsyncLifetime
{
    private const ushort S3Port = 9000;
    private readonly IContainer _container = new ContainerBuilder("rustfs/rustfs:latest")
        .WithPortBinding(S3Port, true)
        .WithEnvironment("AWS_ACCESS_KEY_ID", AccessKey)
        .WithEnvironment("AWS_SECRET_ACCESS_KEY", SecretKey)
        .WithEnvironment("MINIO_ROOT_USER", AccessKey)
        .WithEnvironment("MINIO_ROOT_PASSWORD", SecretKey)
        .WithEnvironment("RUSTFS_ACCESS_KEY_ID", AccessKey)
        .WithEnvironment("RUSTFS_SECRET_ACCESS_KEY", SecretKey)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(S3Port))
        .WithCleanUp(true)
        .Build();

    public const string AccessKey = "rustfsadmin";
    public const string SecretKey = "rustfsadmin";
    public const string BucketName = "modulith-files";

    public string Endpoint => $"http://localhost:{_container.GetMappedPublicPort(S3Port)}";

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
