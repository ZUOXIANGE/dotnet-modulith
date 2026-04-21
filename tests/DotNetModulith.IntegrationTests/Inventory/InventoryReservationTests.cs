using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Contracts.Orders;
using DotNetModulith.IntegrationTests.Fixtures;
using DotNetModulith.Modules.Inventory.Application.Jobs;
using DotNetModulith.Modules.Inventory.Application.Subscribers;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetModulith.IntegrationTests.Inventory;

[Collection("Database collection")]
public class InventoryReservationTests(PostgreSqlFixture dbFixture) : IAsyncLifetime
{
    private DbContextOptions<InventoryDbContext> _options = null!;

    public ValueTask InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(dbFixture.ConnectionString)
            .Options;

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task HandleOrderCreatedAsync_ShouldPersistPendingReservations()
    {
        await dbFixture.ResetAsync();
        var ct = TestContext.Current.CancellationToken;

        await SeedStocksAsync(
            Stock.Create("PROD-001", "Widget", 10),
            Stock.Create("PROD-002", "Bolt", 5));

        await using var dbContext = new InventoryDbContext(_options);
        var subscriber = CreateSubscriber(dbContext);

        var @event = new OrderCreatedIntegrationEvent(
            OrderId: Guid.NewGuid().ToString(),
            CustomerId: "CUST-001",
            TotalAmount: 35m,
            Lines:
            [
                new OrderLineContract("PROD-001", "Widget", 2, 10m),
                new OrderLineContract("PROD-002", "Bolt", 3, 5m)
            ]);

        await subscriber.HandleOrderCreatedAsync(@event, ct);

        await using var assertionDbContext = new InventoryDbContext(_options);
        var reservations = await assertionDbContext.StockReservations
            .OrderBy(x => x.ProductId)
            .ToListAsync(ct);
        var stocks = await assertionDbContext.Stocks
            .OrderBy(x => x.ProductId)
            .ToListAsync(ct);

        reservations.Should().HaveCount(2);
        reservations.Should().OnlyContain(x => x.OrderId == @event.OrderId);
        reservations.Should().OnlyContain(x => x.Status == StockReservationStatus.Pending);

        stocks.Should().ContainSingle(x => x.ProductId == "PROD-001" && x.AvailableQuantity == 8 && x.ReservedQuantity == 2);
        stocks.Should().ContainSingle(x => x.ProductId == "PROD-002" && x.AvailableQuantity == 2 && x.ReservedQuantity == 3);
    }

    [Fact]
    public async Task HandleOrderCancelledAsync_ShouldReleasePendingReservationsPrecisely()
    {
        await dbFixture.ResetAsync();
        var ct = TestContext.Current.CancellationToken;

        await SeedStocksAsync(Stock.Create("PROD-003", "Nut", 6));

        var orderId = Guid.NewGuid().ToString();

        await using (var arrangeDbContext = new InventoryDbContext(_options))
        {
            var subscriber = CreateSubscriber(arrangeDbContext);
            await subscriber.HandleOrderCreatedAsync(
                new OrderCreatedIntegrationEvent(
                    orderId,
                    "CUST-002",
                    12m,
                    [new OrderLineContract("PROD-003", "Nut", 4, 3m)]),
                ct);
        }

        await using (var cancelDbContext = new InventoryDbContext(_options))
        {
            var subscriber = CreateSubscriber(cancelDbContext);
            await subscriber.HandleOrderCancelledAsync(
                new OrderCancelledIntegrationEvent(
                    orderId,
                    "CUST-002",
                    "payment failed",
                    [new OrderLineContract("PROD-003", "Nut", 4, 3m)]),
                ct);
        }

        await using var assertionDbContext = new InventoryDbContext(_options);
        var reservation = await assertionDbContext.StockReservations.SingleAsync(ct);
        var stock = await assertionDbContext.Stocks.SingleAsync(ct);

        reservation.Status.Should().Be(StockReservationStatus.Released);
        stock.AvailableQuantity.Should().Be(6);
        stock.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task HandleOrderPaidAsync_ShouldConfirmPendingReservations()
    {
        await dbFixture.ResetAsync();
        var ct = TestContext.Current.CancellationToken;

        await SeedStocksAsync(Stock.Create("PROD-004", "Screw", 9));

        var orderId = Guid.NewGuid().ToString();

        await using (var arrangeDbContext = new InventoryDbContext(_options))
        {
            var subscriber = CreateSubscriber(arrangeDbContext);
            await subscriber.HandleOrderCreatedAsync(
                new OrderCreatedIntegrationEvent(
                    orderId,
                    "CUST-003",
                    20m,
                    [new OrderLineContract("PROD-004", "Screw", 5, 4m)]),
                ct);
        }

        await using (var paidDbContext = new InventoryDbContext(_options))
        {
            var subscriber = CreateSubscriber(paidDbContext);
            await subscriber.HandleOrderPaidAsync(
                new OrderPaidIntegrationEvent(orderId, "CUST-003", 20m),
                ct);
        }

        await using var assertionDbContext = new InventoryDbContext(_options);
        var reservation = await assertionDbContext.StockReservations.SingleAsync(ct);
        var stock = await assertionDbContext.Stocks.SingleAsync(ct);

        reservation.Status.Should().Be(StockReservationStatus.Confirmed);
        stock.AvailableQuantity.Should().Be(4);
        stock.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task LowStockAlertJob_ShouldPublishEvent_AndMarkStocksAsAlerted()
    {
        await dbFixture.ResetAsync();
        var ct = TestContext.Current.CancellationToken;

        await SeedStocksAsync(
            Stock.Create("PROD-LOW-001", "Sensor", 3),
            Stock.Create("PROD-LOW-002", "Battery", 8),
            Stock.Create("PROD-OK-001", "Cable", 20));

        await using var dbContext = new InventoryDbContext(_options);
        var publisher = new CapturingCapPublisher();
        var job = new LowStockAlertJob(
            new StockRepository(dbContext),
            dbContext,
            publisher,
            Options.Create(new LowStockAlertOptions
            {
                Threshold = 10,
                BatchSize = 20
            }),
            NullLogger<LowStockAlertJob>.Instance);

        await job.ExecuteAsync(new TickerQ.Utilities.Base.TickerFunctionContext(), ct);

        publisher.PublishedMessages.Should().ContainSingle();
        publisher.PublishedMessages[0].Name.Should().Be("modulith.inventory.LowStockDetectedIntegrationEvent");

        var payload = publisher.PublishedMessages[0].Content.Should().BeOfType<LowStockDetectedIntegrationEvent>().Subject;
        payload.Threshold.Should().Be(10);
        payload.Items.Should().HaveCount(2);
        payload.Items.Should().Contain(x => x.ProductId == "PROD-LOW-001" && x.AvailableQuantity == 3);
        payload.Items.Should().Contain(x => x.ProductId == "PROD-LOW-002" && x.AvailableQuantity == 8);

        await using var assertionDbContext = new InventoryDbContext(_options);
        var alertedStocks = await assertionDbContext.Stocks
            .Where(x => x.ProductId.StartsWith("PROD-LOW"))
            .OrderBy(x => x.ProductId)
            .ToListAsync(ct);

        alertedStocks.Should().OnlyContain(x => x.LowStockAlertSentAt != null);
        alertedStocks.Should().OnlyContain(x => x.LastAlertedAvailableQuantity == x.AvailableQuantity);
    }

    private async Task SeedStocksAsync(params Stock[] stocks)
    {
        await using var dbContext = new InventoryDbContext(_options);
        dbContext.Stocks.AddRange(stocks);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static OrderEventSubscriber CreateSubscriber(InventoryDbContext dbContext)
    {
        return new OrderEventSubscriber(
            new StockRepository(dbContext),
            new NullCapPublisher(),
            NullLogger<OrderEventSubscriber>.Instance,
            dbContext);
    }

    private sealed class NullCapPublisher : ICapPublisher
    {
        public IServiceProvider ServiceProvider => null!;

        public ICapTransaction? Transaction { get; set; }

        public Task PublishAsync<T>(string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task PublishAsync<T>(string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Publish<T>(string name, T? contentObj, string? callbackName = null)
        {
        }

        public void Publish<T>(string name, T? contentObj, IDictionary<string, string?> headers)
        {
        }

        public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void PublishDelay<T>(TimeSpan delayTime, string name, T? contentObj, IDictionary<string, string?> headers)
        {
        }

        public void PublishDelay<T>(TimeSpan delayTime, string name, T? contentObj, string? callbackName = null)
        {
        }
    }

    private sealed class CapturingCapPublisher : ICapPublisher
    {
        public List<(string Name, object? Content)> PublishedMessages { get; } = [];

        public IServiceProvider ServiceProvider => null!;

        public ICapTransaction? Transaction { get; set; }

        public Task PublishAsync<T>(string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
        {
            PublishedMessages.Add((name, contentObj));
            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
        {
            PublishedMessages.Add((name, contentObj));
            return Task.CompletedTask;
        }

        public void Publish<T>(string name, T? contentObj, string? callbackName = null)
        {
            PublishedMessages.Add((name, contentObj));
        }

        public void Publish<T>(string name, T? contentObj, IDictionary<string, string?> headers)
        {
            PublishedMessages.Add((name, contentObj));
        }

        public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void PublishDelay<T>(TimeSpan delayTime, string name, T? contentObj, IDictionary<string, string?> headers)
        {
        }

        public void PublishDelay<T>(TimeSpan delayTime, string name, T? contentObj, string? callbackName = null)
        {
        }
    }
}

