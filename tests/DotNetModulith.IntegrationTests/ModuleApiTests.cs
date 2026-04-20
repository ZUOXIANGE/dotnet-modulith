using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.IntegrationTests.Fixtures;
using FluentAssertions;
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

    [Fact]
    public async Task CreateOrder_WithInvalidRequest_ShouldReturnUnifiedErrorResponse()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                CustomerId = "",
                Lines = Array.Empty<object>()
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertValidationErrorContainsAsync(response, "CustomerId");
    }

    [Fact]
    public async Task CreateStock_WithInvalidRequest_ShouldReturnUnifiedErrorResponse()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/inventory/stocks",
            new
            {
                ProductId = "",
                ProductName = "",
                InitialQuantity = -1
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertValidationErrorContainsAsync(response, "InitialQuantity");
    }

    [Fact]
    public async Task ReplenishStock_WithInvalidQuantity_ShouldReturnUnifiedErrorResponse()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/inventory/stocks/PROD-001/replenish",
            new
            {
                Quantity = 0
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertValidationErrorContainsAsync(response, "Quantity");
    }

    private static async Task AssertValidationErrorContainsAsync(HttpResponseMessage response, string fieldName)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!["msg"]!.GetValue<string>().Should().Be("validation failed");
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.ValidationFailed);
        body!["data"]!["errors"].Should().NotBeNull();
        body!["data"]!["errors"]![fieldName].Should().NotBeNull();
    }
}
