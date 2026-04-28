using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace DotNetModulith.IntegrationTests;

[Collection("Api collection")]
public sealed class OrdersMcpTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrdersMcpTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task McpInitialize_AndListTools_ShouldSucceed()
    {
        await _factory.InitializeUsersModuleAsync();
        var token = await LoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var initializeResponse = await SendMcpAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "initialize",
            ["params"] = new JsonObject
            {
                ["protocolVersion"] = "2024-11-05",
                ["capabilities"] = new JsonObject(),
                ["clientInfo"] = new JsonObject
                {
                    ["name"] = "integration-tests",
                    ["version"] = "1.0.0"
                }
            }
        });

        initializeResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        initializeResponse.Headers.TryGetValues("Mcp-Session-Id", out var values).Should().BeTrue();
        var sessionId = values!.Single();

        var listToolsResponse = await SendMcpAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 2,
            ["method"] = "tools/list"
        }, sessionId);

        listToolsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await listToolsResponse.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        var tools = body!["result"]!["tools"]!.AsArray().Select(x => x!["name"]!.GetValue<string>()).ToArray();
        tools.Should().Contain(["create_order", "confirm_order", "get_order"]);
    }

    [Fact]
    public async Task McpCreateAndGetOrder_ShouldSucceed()
    {
        await _factory.InitializeUsersModuleAsync();
        var token = await LoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var sessionId = await InitializeMcpSessionAsync();
        var createResult = await CallToolAsync(sessionId, "create_order", new JsonObject
        {
            ["customerId"] = "MCP-CUSTOMER-01",
            ["lines"] = new JsonArray
            {
                new JsonObject
                {
                    ["productId"] = "PROD-MCP-001",
                    ["productName"] = "MCP Widget",
                    ["quantity"] = 2,
                    ["unitPrice"] = 19.9m
                }
            }
        });

        createResult["isError"]?.GetValue<bool>().Should().NotBe(true);
        var createData = JsonSerializer.Deserialize<JsonObject>(createResult["content"]![0]!["text"]!.GetValue<string>(), JsonOptions)!;
        var orderId = createData["orderId"]!.GetValue<string>();

        var getResult = await CallToolAsync(sessionId, "get_order", new JsonObject
        {
            ["orderId"] = orderId
        });

        getResult["isError"]?.GetValue<bool>().Should().NotBe(true);
        var orderData = JsonSerializer.Deserialize<JsonObject>(getResult["content"]![0]!["text"]!.GetValue<string>(), JsonOptions)!;
        orderData["orderId"]!.GetValue<string>().Should().Be(orderId);
        orderData["customerId"]!.GetValue<string>().Should().Be("MCP-CUSTOMER-01");
    }

    [Fact]
    public async Task McpWithoutToken_ShouldBeUnauthorized()
    {
        await _factory.InitializeUsersModuleAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await SendMcpAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "initialize",
            ["params"] = new JsonObject
            {
                ["protocolVersion"] = "2024-11-05",
                ["capabilities"] = new JsonObject(),
                ["clientInfo"] = new JsonObject
                {
                    ["name"] = "integration-tests",
                    ["version"] = "1.0.0"
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> InitializeMcpSessionAsync()
    {
        var response = await SendMcpAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "initialize",
            ["params"] = new JsonObject
            {
                ["protocolVersion"] = "2024-11-05",
                ["capabilities"] = new JsonObject(),
                ["clientInfo"] = new JsonObject
                {
                    ["name"] = "integration-tests",
                    ["version"] = "1.0.0"
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Headers.TryGetValues("Mcp-Session-Id", out var values).Should().BeTrue();
        return values!.Single();
    }

    private async Task<JsonObject> CallToolAsync(string sessionId, string toolName, JsonObject arguments)
    {
        var response = await SendMcpAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 3,
            ["method"] = "tools/call",
            ["params"] = new JsonObject
            {
                ["name"] = toolName,
                ["arguments"] = arguments
            }
        }, sessionId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        return body!["result"]!.AsObject();
    }

    private async Task<HttpResponseMessage> SendMcpAsync(JsonObject payload, string? sessionId = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            request.Headers.Add("Mcp-Session-Id", sessionId);
        }

        request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<string> LoginAsAdminAsync()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
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