using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Api.MultiTenancy;
using DotNetModulith.IntegrationTests.Fixtures;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetModulith.IntegrationTests;

[Collection("Api collection")]
public sealed class TenancyApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApiWebApplicationFactory _factory;

    public TenancyApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCurrentTenant_ShouldResolveTenant_AndPersistContextToDistributedCache()
    {
        await _factory.InitializeUsersModuleAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TenantTestData.HeaderName, TenantTestData.TenantA);

        var response = await client.GetAsync("/api/tenancy/current", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        body["data"]!["resolved"]!.GetValue<bool>().Should().BeTrue();
        body["data"]!["identifier"]!.GetValue<string>().Should().Be(TenantTestData.TenantA);

        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var payload = await cache.GetStringAsync(
            DistributedTenantContextStore.BuildCacheKey(TenantTestData.TenantA),
            TestContext.Current.CancellationToken);

        payload.Should().NotBeNull();
        var snapshot = JsonSerializer.Deserialize<TenantContextSnapshot>(payload!, JsonOptions);
        snapshot.Should().NotBeNull();
        snapshot!.TenantIdentifier.Should().Be(TenantTestData.TenantA);
    }

    [Fact]
    public async Task GetOrder_ShouldRespectTenantIsolation()
    {
        await _factory.InitializeUsersModuleAsync();

        var orderId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;

        await using (var tenantAContext = TenantTestData.CreateOrdersDbContext(options, TenantTestData.TenantA))
        {
            tenantAContext.Orders.Add(new OrderEntity
            {
                Id = orderId,
                TenantId = TenantTestData.TenantA,
                CustomerId = "TENANT-A-CUST",
                Status = OrderStatus.Pending,
                TotalAmount = 88m,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await tenantAContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var token = await LoginAsAdminAsync(TenantTestData.TenantA);

        using var tenantAClient = _factory.CreateClient();
        tenantAClient.DefaultRequestHeaders.Add(TenantTestData.HeaderName, TenantTestData.TenantA);
        tenantAClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tenantAResponse = await tenantAClient.GetAsync(
            $"/api/orders/{orderId}",
            TestContext.Current.CancellationToken);

        tenantAResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenantABody = await tenantAResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        tenantABody!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        tenantABody["data"]!["orderId"]!.GetValue<string>().Should().Be(orderId.ToString());

        using var tenantBClient = _factory.CreateClient();
        tenantBClient.DefaultRequestHeaders.Add(TenantTestData.HeaderName, TenantTestData.TenantB);
        tenantBClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tenantBResponse = await tenantBClient.GetAsync(
            $"/api/orders/{orderId}",
            TestContext.Current.CancellationToken);

        tenantBResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenantBBody = await tenantBResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        tenantBBody!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.NotFound);
    }

    private async Task<string> LoginAsAdminAsync(string tenantIdentifier)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TenantTestData.HeaderName, tenantIdentifier);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                UserName = "admin",
                Password = "Admin@123456"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        return body["data"]!["accessToken"]!.GetValue<string>();
    }
}
