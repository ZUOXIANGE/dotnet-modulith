using DotNetModulith.Modules.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// 使用真实 PostgreSQL 与 RabbitMQ 的 WebApplicationFactory，用于验证 CAP 闭环
/// </summary>
public sealed class MessagingApiWebApplicationFactory : TestWebApplicationFactoryBase
{
    private readonly RabbitMqFixture _rabbitMqFixture = new();
    private TestEnvironmentVariables? _runtimeEnvironmentVariables;
    private bool _usersModuleInitialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Integration");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
        });
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await _rabbitMqFixture.InitializeAsync();

        _runtimeEnvironmentVariables = MessagingTestRuntimeConfiguration.Create(ConnectionString, _rabbitMqFixture);
        _runtimeEnvironmentVariables.Apply();
    }

    public async Task ResetDatabaseAsync()
    {
        await DbFixture.ResetAsync();
        _usersModuleInitialized = false;
    }

    public async Task InitializeUsersModuleAsync()
    {
        if (_usersModuleInitialized)
        {
            return;
        }

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.SeedUsersModuleAsync();
        _usersModuleInitialized = true;
    }

    public override async ValueTask DisposeAsync()
    {
        _runtimeEnvironmentVariables?.Clear();

        await _rabbitMqFixture.DisposeAsync();
        await base.DisposeAsync();
    }
}
