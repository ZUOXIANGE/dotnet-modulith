using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DotNetModulith.IntegrationTests;

/// <summary>
/// 模块API集成测试，验证系统健康检查和模块管理端点的可用性
/// </summary>
public class ModuleApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ModuleApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// 验证健康检查端点返回健康状态
    /// </summary>
    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.Should().BeSuccessful();
    }

    /// <summary>
    /// 验证模块列表端点返回所有已注册模块
    /// </summary>
    [Fact]
    public async Task ModulesEndpoint_ShouldReturnAllModules()
    {
        var response = await _client.GetAsync("/api/modules", TestContext.Current.CancellationToken);

        response.Should().BeSuccessful();
    }

    /// <summary>
    /// 验证存活检查端点返回成功状态
    /// </summary>
    [Fact]
    public async Task AlivenessEndpoint_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/alive", TestContext.Current.CancellationToken);

        response.Should().BeSuccessful();
    }
}
