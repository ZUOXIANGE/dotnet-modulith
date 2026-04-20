using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// 集成测试 WebApplicationFactory 基类，统一管理 PostgreSQL 测试夹具生命周期
/// </summary>
public abstract class TestWebApplicationFactoryBase : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected PostgreSqlFixture DbFixture { get; } = new();

    public string ConnectionString => DbFixture.ConnectionString;

    public virtual async ValueTask InitializeAsync()
    {
        await DbFixture.InitializeAsync();
    }

    public new virtual async ValueTask DisposeAsync()
    {
        await DbFixture.DisposeAsync();
        await base.DisposeAsync();
    }
}
