using System.Net;
using System.Net.Http.Headers;
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
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ModuleApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
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
        await AuthorizeAsAdminAsync();
        var response = await _client.GetAsync("/api/modules", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
    }

    [Fact]
    public async Task ModuleGraphEndpoint_ShouldReturnDependencyGraph()
    {
        await AuthorizeAsAdminAsync();
        var response = await _client.GetAsync("/api/modules/graph", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
    }

    [Fact]
    public async Task ModuleVerifyEndpoint_ShouldReturnBoundaryStatus()
    {
        await AuthorizeAsAdminAsync();
        var response = await _client.GetAsync("/api/modules/verify", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturnUnauthorizedCode()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/modules", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertBusinessCodeAsync(response, ApiCodes.Common.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutPermission_ShouldReturnForbiddenCode()
    {
        await _factory.InitializeUsersModuleAsync();

        var adminAccessToken = await LoginAsAdminAsync();
        var user = await CreateUserAsync(adminAccessToken);
        var userAccessToken = await LoginAsync(user.UserName, user.Password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken);

        var response = await _client.GetAsync("/api/modules", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertBusinessCodeAsync(response, ApiCodes.Common.Forbidden);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidRequest_ShouldReturnUnifiedErrorResponse()
    {
        await AuthorizeAsAdminAsync();

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
        await AuthorizeAsAdminAsync();

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
        await AuthorizeAsAdminAsync();

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

    private static async Task AssertBusinessCodeAsync(HttpResponseMessage response, int code)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!["code"]!.GetValue<int>().Should().Be(code);
    }

    private async Task AuthorizeAsAdminAsync()
    {
        await _factory.InitializeUsersModuleAsync();
        var accessToken = await LoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<string> LoginAsAdminAsync()
        => await LoginAsync("admin", "Admin@123456");

    private async Task<string> LoginAsync(string userName, string password)
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                UserName = userName,
                Password = password
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        return body["data"]!["accessToken"]!.GetValue<string>();
    }

    private async Task<(Guid UserId, string UserName, string Password)> CreateUserAsync(string adminAccessToken)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userName = $"rbac_{suffix}";
        const string password = "User@123456";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);

        var response = await _client.PostAsJsonAsync(
            "/api/users",
            new
            {
                UserName = userName,
                DisplayName = $"RBAC User {suffix}",
                Email = $"{suffix}@modulith.local",
                Password = password,
                RoleIds = Array.Empty<Guid>()
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        return (body["data"]!["id"]!.GetValue<Guid>(), userName, password);
    }
}

