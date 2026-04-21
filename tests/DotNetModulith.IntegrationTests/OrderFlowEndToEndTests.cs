using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Contracts.Orders;
using DotNetModulith.Abstractions.Contracts.Payments;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.IntegrationTests.Fixtures;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using DotNetModulith.Modules.Payments.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetModulith.IntegrationTests;

[Collection("Messaging api collection")]
public sealed class OrderFlowEndToEndTests : IClassFixture<MessagingApiWebApplicationFactory>
{
    private readonly MessagingApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrderFlowEndToEndTests(MessagingApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_WithSufficientStock_ShouldEventuallyBecomePaid()
    {
        await _factory.ResetDatabaseAsync();
        await AuthorizeAsAdminAsync();
        var ct = TestContext.Current.CancellationToken;
        var productId = $"PROD-{Guid.NewGuid():N}"[..12];

        var createStockResponse = await _client.PostAsJsonAsync(
            "/api/inventory/stocks",
            new
            {
                ProductId = productId,
                ProductName = "E2E Widget",
                InitialQuantity = 10
            },
            ct);

        createStockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createOrderResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                CustomerId = "E2E-CUSTOMER-01",
                Lines = new[]
                {
                    new
                    {
                        ProductId = productId,
                        ProductName = "E2E Widget",
                        Quantity = 2,
                        UnitPrice = 15.5m
                    }
                }
            },
            ct);

        createOrderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderCreation = await createOrderResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<CreateOrderResult>>(cancellationToken: ct);

        orderCreation.Should().NotBeNull();
        orderCreation!.Code.Should().Be(ApiCodes.Common.Success);
        orderCreation.Data.Should().NotBeNull();

        var orderId = orderCreation.Data!.OrderId;

        var payment = await PollAsync(
            async () =>
            {
                await using var dbContext = new PaymentsDbContext(
                    new DbContextOptionsBuilder<PaymentsDbContext>()
                        .UseNpgsql(_factory.ConnectionString)
                        .Options);

                return await dbContext.Payments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OrderId == orderId, ct);
            },
            result => result.Status.ToString() == "Completed",
            TimeSpan.FromSeconds(30),
            ct);

        payment.Status.ToString().Should().Be("Completed");

        var order = await PollAsync(
            async () =>
            {
                var response = await _client.GetFromJsonAsync<ApiEnvelope<OrderDetailResult>>(
                    $"/api/orders/{orderId}",
                    ct);

                return response?.Data;
            },
            result => result.Status == "Paid",
            TimeSpan.FromSeconds(30),
            ct);

        order.Status.Should().Be("Paid");

        var stock = await PollAsync(
            async () =>
            {
                var response = await _client.GetFromJsonAsync<ApiEnvelope<StockDetailResult>>(
                    $"/api/inventory/stocks/{productId}",
                    ct);

                return response?.Data;
            },
            result => result.AvailableQuantity == 8 && result.ReservedQuantity == 0,
            TimeSpan.FromSeconds(30),
            ct);

        stock.AvailableQuantity.Should().Be(8);
        stock.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_ShouldEventuallyBecomeCancelled()
    {
        await _factory.ResetDatabaseAsync();
        await AuthorizeAsAdminAsync();
        var ct = TestContext.Current.CancellationToken;
        var productId = $"PROD-{Guid.NewGuid():N}"[..12];

        var createStockResponse = await _client.PostAsJsonAsync(
            "/api/inventory/stocks",
            new
            {
                ProductId = productId,
                ProductName = "E2E Low Stock",
                InitialQuantity = 1
            },
            ct);

        createStockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createOrderResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                CustomerId = "E2E-CUSTOMER-02",
                Lines = new[]
                {
                    new
                    {
                        ProductId = productId,
                        ProductName = "E2E Low Stock",
                        Quantity = 2,
                        UnitPrice = 9.9m
                    }
                }
            },
            ct);

        createOrderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderCreation = await createOrderResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<CreateOrderResult>>(cancellationToken: ct);

        orderCreation.Should().NotBeNull();
        orderCreation!.Data.Should().NotBeNull();

        var orderId = orderCreation.Data!.OrderId;

        var order = await PollAsync(
            async () =>
            {
                var response = await _client.GetFromJsonAsync<ApiEnvelope<OrderDetailResult>>(
                    $"/api/orders/{orderId}",
                    ct);

                return response?.Data;
            },
            result => result.Status == "Cancelled",
            TimeSpan.FromSeconds(30),
            ct);

        order.Status.Should().Be("Cancelled");

        var stock = await PollAsync(
            async () =>
            {
                var response = await _client.GetFromJsonAsync<ApiEnvelope<StockDetailResult>>(
                    $"/api/inventory/stocks/{productId}",
                    ct);

                return response?.Data;
            },
            result => result.AvailableQuantity == 1 && result.ReservedQuantity == 0,
            TimeSpan.FromSeconds(30),
            ct);

        stock.AvailableQuantity.Should().Be(1);
        stock.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task DuplicatePaymentCompletedEvent_ShouldKeepOrderAndStockStable()
    {
        await _factory.ResetDatabaseAsync();
        await AuthorizeAsAdminAsync();
        var ct = TestContext.Current.CancellationToken;
        var productId = $"PROD-{Guid.NewGuid():N}"[..12];

        await CreateStockAsync(productId, "Idempotent Widget", 10, ct);
        var orderId = await CreateOrderAsync(productId, "Idempotent Widget", 2, 15.5m, "E2E-IDEMPOTENT-01", ct);

        var payment = await PollAsync(
            async () => await LoadPaymentByOrderIdAsync(orderId, ct),
            result => result.Status.ToString() == "Completed",
            TimeSpan.FromSeconds(30),
            ct);

        await PublishAsync(
            "modulith.payments.PaymentCompletedIntegrationEvent",
            new PaymentCompletedIntegrationEvent(orderId, payment.Id.ToString(), "E2E-IDEMPOTENT-01", 31m),
            ct);

        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var order = await GetOrderAsync(orderId, ct);
        var stock = await GetStockAsync(productId, ct);
        var paymentsForOrder = await CountPaymentsByOrderIdAsync(orderId, ct);

        order.Status.Should().Be("Paid");
        stock.AvailableQuantity.Should().Be(8);
        stock.ReservedQuantity.Should().Be(0);
        paymentsForOrder.Should().Be(1);
    }

    [Fact]
    public async Task DuplicateStockReservedEvent_ShouldNotCreateSecondPaymentOrReprocessOrder()
    {
        await _factory.ResetDatabaseAsync();
        await AuthorizeAsAdminAsync();
        var ct = TestContext.Current.CancellationToken;
        var productId = $"PROD-{Guid.NewGuid():N}"[..12];

        await CreateStockAsync(productId, "Duplicate Reserve Widget", 10, ct);
        var orderId = await CreateOrderAsync(productId, "Duplicate Reserve Widget", 2, 15.5m, "E2E-IDEMPOTENT-02", ct);

        var payment = await PollAsync(
            async () => await LoadPaymentByOrderIdAsync(orderId, ct),
            result => result.Status.ToString() == "Completed",
            TimeSpan.FromSeconds(30),
            ct);

        await PublishAsync(
            "modulith.inventory.StockReservedIntegrationEvent",
            new StockReservedIntegrationEvent(
                orderId,
                "E2E-IDEMPOTENT-02",
                31m,
                [new StockReservedLine(productId, 2)]),
            ct);

        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var order = await GetOrderAsync(orderId, ct);
        var stock = await GetStockAsync(productId, ct);
        var paymentsForOrder = await CountPaymentsByOrderIdAsync(orderId, ct);
        var completedPayment = await LoadPaymentByOrderIdAsync(orderId, ct);

        order.Status.Should().Be("Paid");
        stock.AvailableQuantity.Should().Be(8);
        stock.ReservedQuantity.Should().Be(0);
        paymentsForOrder.Should().Be(1);
        completedPayment.Should().NotBeNull();
        completedPayment!.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task PaymentCompletedEvent_ArrivingBeforeOrderConfirmation_ShouldBeIgnored()
    {
        await _factory.ResetDatabaseAsync();
        await AuthorizeAsAdminAsync();
        var ct = TestContext.Current.CancellationToken;

        var orderId = await CreatePendingOrderAsync(
            "E2E-OUTOFORDER-01",
            [new OrderLineData("PROD-OOO-001", "Out Of Order Widget", 2, 12.5m)],
            ct);

        await PublishAsync(
            "modulith.payments.PaymentCompletedIntegrationEvent",
            new PaymentCompletedIntegrationEvent(orderId, Guid.NewGuid().ToString(), "E2E-OUTOFORDER-01", 25m),
            ct);

        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var order = await GetOrderAsync(orderId, ct);

        order.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task PaymentFailedEvent_AfterOrderPaid_ShouldNotRollbackOrderOrStock()
    {
        await _factory.ResetDatabaseAsync();
        await AuthorizeAsAdminAsync();
        var ct = TestContext.Current.CancellationToken;
        var productId = $"PROD-{Guid.NewGuid():N}"[..12];

        await CreateStockAsync(productId, "Late Failure Widget", 10, ct);
        var orderId = await CreateOrderAsync(productId, "Late Failure Widget", 2, 15.5m, "E2E-OUTOFORDER-02", ct);

        var payment = await PollAsync(
            async () => await LoadPaymentByOrderIdAsync(orderId, ct),
            result => result.Status == "Completed",
            TimeSpan.FromSeconds(30),
            ct);

        var orderBeforeReplay = await PollAsync(
            async () =>
            {
                var response = await _client.GetFromJsonAsync<ApiEnvelope<OrderDetailResult>>(
                    $"/api/orders/{orderId}",
                    ct);

                return response?.Data;
            },
            result => result.Status == "Paid",
            TimeSpan.FromSeconds(30),
            ct);

        orderBeforeReplay.Status.Should().Be("Paid");

        await PublishAsync(
            "modulith.payments.PaymentFailedIntegrationEvent",
            new PaymentFailedIntegrationEvent(orderId, payment.Id.ToString(), "E2E-OUTOFORDER-02", "late failure replay"),
            ct);

        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var order = await GetOrderAsync(orderId, ct);
        var stock = await GetStockAsync(productId, ct);
        var paymentsForOrder = await CountPaymentsByOrderIdAsync(orderId, ct);

        order.Status.Should().Be("Paid");
        stock.AvailableQuantity.Should().Be(8);
        stock.ReservedQuantity.Should().Be(0);
        paymentsForOrder.Should().Be(1);
    }

    private async Task CreateStockAsync(string productId, string productName, int initialQuantity, CancellationToken ct)
    {
        var createStockResponse = await _client.PostAsJsonAsync(
            "/api/inventory/stocks",
            new
            {
                ProductId = productId,
                ProductName = productName,
                InitialQuantity = initialQuantity
            },
            ct);

        createStockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<string> CreateOrderAsync(
        string productId,
        string productName,
        int quantity,
        decimal unitPrice,
        string customerId,
        CancellationToken ct)
    {
        var createOrderResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                CustomerId = customerId,
                Lines = new[]
                {
                    new
                    {
                        ProductId = productId,
                        ProductName = productName,
                        Quantity = quantity,
                        UnitPrice = unitPrice
                    }
                }
            },
            ct);

        createOrderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderCreation = await createOrderResponse.Content
            .ReadFromJsonAsync<ApiEnvelope<CreateOrderResult>>(cancellationToken: ct);

        orderCreation.Should().NotBeNull();
        orderCreation!.Code.Should().Be(ApiCodes.Common.Success);
        orderCreation.Data.Should().NotBeNull();

        return orderCreation.Data!.OrderId;
    }

    private async Task AuthorizeAsAdminAsync()
    {
        await _factory.InitializeUsersModuleAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsAdminAsync());
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
        body.Should().NotBeNull();
        body!["code"]!.GetValue<int>().Should().Be(ApiCodes.Common.Success);
        return body["data"]!["accessToken"]!.GetValue<string>();
    }

    private async Task<string> CreatePendingOrderAsync(
        string customerId,
        IReadOnlyList<OrderLineData> lines,
        CancellationToken ct)
    {
        var order = Order.Create(customerId, lines);
        order.ClearDomainEvents();

        await using var dbContext = CreateOrdersDbContext();
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(ct);

        return order.Id.ToString();
    }

    private async Task PublishAsync<T>(string topic, T payload, CancellationToken ct)
    {
        using var scope = _factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<ICapPublisher>();
        await publisher.PublishAsync(topic, payload, cancellationToken: ct);
    }

    private async Task<PaymentDetailRecord?> LoadPaymentByOrderIdAsync(string orderId, CancellationToken ct)
    {
        await using var dbContext = CreatePaymentsDbContext();
        return await dbContext.Payments
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .Select(x => new PaymentDetailRecord(x.Id.Value, x.OrderId, x.Status.ToString()))
            .FirstOrDefaultAsync(ct);
    }

    private async Task<int> CountPaymentsByOrderIdAsync(string orderId, CancellationToken ct)
    {
        await using var dbContext = CreatePaymentsDbContext();
        return await dbContext.Payments
            .AsNoTracking()
            .CountAsync(x => x.OrderId == orderId, ct);
    }

    private async Task<OrderDetailResult> GetOrderAsync(string orderId, CancellationToken ct)
    {
        return await PollAsync(
            async () =>
            {
                var response = await _client.GetFromJsonAsync<ApiEnvelope<OrderDetailResult>>(
                    $"/api/orders/{orderId}",
                    ct);

                return response?.Data;
            },
            _ => true,
            TimeSpan.FromSeconds(10),
            ct);
    }

    private async Task<StockDetailResult> GetStockAsync(string productId, CancellationToken ct)
    {
        return await PollAsync(
            async () =>
            {
                var response = await _client.GetFromJsonAsync<ApiEnvelope<StockDetailResult>>(
                    $"/api/inventory/stocks/{productId}",
                    ct);

                return response?.Data;
            },
            _ => true,
            TimeSpan.FromSeconds(10),
            ct);
    }

    private PaymentsDbContext CreatePaymentsDbContext()
    {
        return new PaymentsDbContext(
            new DbContextOptionsBuilder<PaymentsDbContext>()
                .UseNpgsql(_factory.ConnectionString)
                .Options);
    }

    private OrdersDbContext CreateOrdersDbContext()
    {
        return new OrdersDbContext(
            new DbContextOptionsBuilder<OrdersDbContext>()
                .UseNpgsql(_factory.ConnectionString)
                .Options);
    }

    private static async Task<T> PollAsync<T>(
        Func<Task<T?>> probe,
        Func<T, bool> predicate,
        TimeSpan timeout,
        CancellationToken ct)
        where T : class
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        T? lastSeen = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var result = await probe();
            if (result is not null)
            {
                lastSeen = result;
                if (predicate(result))
                {
                    return result;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        }

        throw new TimeoutException(
            $"Condition was not met within {timeout.TotalSeconds} seconds. Last seen: {JsonSerializer.Serialize(lastSeen)}");
    }

    private sealed record ApiEnvelope<T>(string Msg, int Code, T? Data);

    private sealed record CreateOrderResult(string OrderId);

    private sealed record OrderDetailResult(
        string OrderId,
        string CustomerId,
        string Status,
        decimal TotalAmount);

    private sealed record StockDetailResult(
        string ProductId,
        string ProductName,
        int AvailableQuantity,
        int ReservedQuantity);

    private sealed record PaymentDetailRecord(Guid Id, string OrderId, string Status);
}

