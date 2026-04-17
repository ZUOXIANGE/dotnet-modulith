using System.Net;
using DotNetCore.CAP;
using DotNetModulith.IntegrationTests.Fixtures;
using DotNetModulith.Modules.Inventory.Infrastructure;
using DotNetModulith.Modules.Orders.Infrastructure;
using DotNetModulith.Modules.Payments.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetModulith.IntegrationTests;

[Collection("Api collection")]
public class ModuleApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ModuleApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AlivenessEndpoint_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/alive", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ModulesEndpoint_ShouldReturnAllModules()
    {
        var response = await _client.GetAsync("/api/modules", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ModuleGraphEndpoint_ShouldReturnDependencyGraph()
    {
        var response = await _client.GetAsync("/api/modules/graph", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ModuleVerifyEndpoint_ShouldReturnBoundaryStatus()
    {
        var response = await _client.GetAsync("/api/modules/verify", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

/// <summary>
/// 自定义WebApplicationFactory，使用Testcontainers PostgreSQL替代真实数据库连接，
/// 并在测试环境中注册CAP的空实现以避免对RabbitMQ的依赖
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _dbFixture = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:modulithdb"] = _dbFixture.ConnectionString,
            });
        });

        builder.ConfigureServices(services =>
        {
            ReplaceDbContext<OrdersDbContext>(services);
            ReplaceDbContext<InventoryDbContext>(services);
            ReplaceDbContext<PaymentsDbContext>(services);

            services.AddSingleton<ICapPublisher, NullCapPublisher>();
        });
    }

    private void ReplaceDbContext<TDbContext>(IServiceCollection services) where TDbContext : DbContext
    {
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<TDbContext>));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<TDbContext>(options =>
        {
            options.UseNpgsql(_dbFixture.ConnectionString);
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _dbFixture.InitializeAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await _dbFixture.DisposeAsync();
        await base.DisposeAsync();
    }
}

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

[CollectionDefinition("Api collection")]
public class ApiCollection : ICollectionFixture<ApiWebApplicationFactory>;
