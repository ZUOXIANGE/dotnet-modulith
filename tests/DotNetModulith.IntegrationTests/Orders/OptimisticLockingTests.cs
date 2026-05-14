using DotNetModulith.IntegrationTests.Fixtures;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Infrastructure;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using DotNetModulith.Modules.Payments.Domain;
using DotNetModulith.Modules.Payments.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetModulith.IntegrationTests.Orders;

[Collection("Database collection")]
public class OptimisticLockingTests(PostgreSqlFixture dbFixture) : IAsyncLifetime
{
    private DbContextOptions<OrdersDbContext> _ordersOptions = null!;
    private DbContextOptions<InventoryDbContext> _inventoryOptions = null!;
    private DbContextOptions<PaymentsDbContext> _paymentsOptions = null!;

    public ValueTask InitializeAsync()
    {
        _ordersOptions = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql(dbFixture.ConnectionString)
            .Options;

        _inventoryOptions = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(dbFixture.ConnectionString)
            .Options;

        _paymentsOptions = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(dbFixture.ConnectionString)
            .Options;

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ConcurrentOrderUpdate_ShouldThrowDbUpdateConcurrencyException()
    {
        await dbFixture.ResetAsync();
        var ct = TestContext.Current.CancellationToken;

        var orderId = Guid.NewGuid();
        await using (var seedContext = new OrdersDbContext(_ordersOptions))
        {
            seedContext.Orders.Add(new OrderEntity
            {
                Id = orderId,
                CustomerId = "CONCURRENT-001",
                Status = OrderStatus.Pending,
                TotalAmount = 100m,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync(ct);
        }

        await using var contextA = new OrdersDbContext(_ordersOptions);
        await using var contextB = new OrdersDbContext(_ordersOptions);

        var orderA = await contextA.Orders.FindAsync([orderId], ct);
        var orderB = await contextB.Orders.FindAsync([orderId], ct);

        orderA!.Status = OrderStatus.Confirmed;
        orderA.UpdatedAt = DateTimeOffset.UtcNow;
        await contextA.SaveChangesAsync(ct);

        orderB!.Status = OrderStatus.Cancelled;
        orderB.UpdatedAt = DateTimeOffset.UtcNow;

        Func<Task> act = async () => await contextB.SaveChangesAsync(ct);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task RowVersion_ShouldChangeAfterEachUpdate()
    {
        await dbFixture.ResetAsync();
        var ct = TestContext.Current.CancellationToken;

        var orderId = Guid.NewGuid();
        await using (var seedContext = new OrdersDbContext(_ordersOptions))
        {
            seedContext.Orders.Add(new OrderEntity
            {
                Id = orderId,
                CustomerId = "VERSION-001",
                Status = OrderStatus.Pending,
                TotalAmount = 50m,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync(ct);
        }

        byte[] initialVersion;
        await using (var context = new OrdersDbContext(_ordersOptions))
        {
            var order = await context.Orders.FindAsync([orderId], ct);
            initialVersion = order!.RowVersion;
        }

        await using (var context = new OrdersDbContext(_ordersOptions))
        {
            var order = await context.Orders.FindAsync([orderId], ct);
            order!.Status = OrderStatus.Confirmed;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(ct);
        }

        await using (var context = new OrdersDbContext(_ordersOptions))
        {
            var order = await context.Orders.FindAsync([orderId], ct);
            var updatedVersion = order!.RowVersion;

            updatedVersion.Should().NotEqual(initialVersion);
        }
    }

    [Fact]
    public async Task ConcurrentStockReservation_ShouldThrowDbUpdateConcurrencyException()
    {
        await dbFixture.ResetAsync();
        var ct = TestContext.Current.CancellationToken;

        var stockId = Guid.NewGuid();
        await using (var seedContext = new InventoryDbContext(_inventoryOptions))
        {
            seedContext.Stocks.Add(new StockEntity
            {
                Id = stockId,
                ProductId = "STOCK-CONCURRENT-001",
                ProductName = "Concurrent Widget",
                AvailableQuantity = 100,
                ReservedQuantity = 0,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync(ct);
        }

        await using var contextA = new InventoryDbContext(_inventoryOptions);
        await using var contextB = new InventoryDbContext(_inventoryOptions);

        var stockA = await contextA.Stocks.FindAsync([stockId], ct);
        var stockB = await contextB.Stocks.FindAsync([stockId], ct);

        stockA!.AvailableQuantity -= 10;
        stockA.ReservedQuantity += 10;
        stockA.UpdatedAt = DateTimeOffset.UtcNow;
        await contextA.SaveChangesAsync(ct);

        stockB!.AvailableQuantity -= 5;
        stockB.ReservedQuantity += 5;
        stockB.UpdatedAt = DateTimeOffset.UtcNow;

        Func<Task> act = async () => await contextB.SaveChangesAsync(ct);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task ConcurrentPaymentCompletion_ShouldThrowDbUpdateConcurrencyException()
    {
        await dbFixture.ResetAsync();
        var ct = TestContext.Current.CancellationToken;

        var paymentId = Guid.NewGuid();
        await using (var seedContext = new PaymentsDbContext(_paymentsOptions))
        {
            seedContext.Payments.Add(new PaymentEntity
            {
                Id = paymentId,
                OrderId = Guid.NewGuid().ToString(),
                CustomerId = "PAY-CONCURRENT-001",
                Amount = 200m,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync(ct);
        }

        await using var contextA = new PaymentsDbContext(_paymentsOptions);
        await using var contextB = new PaymentsDbContext(_paymentsOptions);

        var paymentA = await contextA.Payments.FindAsync([paymentId], ct);
        var paymentB = await contextB.Payments.FindAsync([paymentId], ct);

        paymentA!.Status = PaymentStatus.Completed;
        paymentA.CompletedAt = DateTimeOffset.UtcNow;
        await contextA.SaveChangesAsync(ct);

        paymentB!.Status = PaymentStatus.Failed;
        paymentB.CompletedAt = DateTimeOffset.UtcNow;

        Func<Task> act = async () => await contextB.SaveChangesAsync(ct);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task RetryAfterConcurrencyException_ShouldSucceedWithFreshData()
    {
        await dbFixture.ResetAsync();
        var ct = TestContext.Current.CancellationToken;

        var orderId = Guid.NewGuid();
        await using (var seedContext = new OrdersDbContext(_ordersOptions))
        {
            seedContext.Orders.Add(new OrderEntity
            {
                Id = orderId,
                CustomerId = "RETRY-001",
                Status = OrderStatus.Pending,
                TotalAmount = 75m,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync(ct);
        }

        await using var contextA = new OrdersDbContext(_ordersOptions);
        await using var contextB = new OrdersDbContext(_ordersOptions);

        var orderA = await contextA.Orders.FindAsync([orderId], ct);
        var orderB = await contextB.Orders.FindAsync([orderId], ct);

        orderA!.Status = OrderStatus.Confirmed;
        orderA.UpdatedAt = DateTimeOffset.UtcNow;
        await contextA.SaveChangesAsync(ct);

        orderB!.Status = OrderStatus.Cancelled;
        orderB.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await contextB.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            foreach (var entry in contextB.ChangeTracker.Entries())
            {
                await entry.ReloadAsync(ct);
            }

            var reloadedOrder = await contextB.Orders.FindAsync([orderId], ct);
            reloadedOrder!.Status.Should().Be(OrderStatus.Confirmed);

            reloadedOrder.Status = OrderStatus.Paid;
            reloadedOrder.UpdatedAt = DateTimeOffset.UtcNow;
            await contextB.SaveChangesAsync(ct);
        }

        await using var verifyContext = new OrdersDbContext(_ordersOptions);
        var finalOrder = await verifyContext.Orders.FindAsync([orderId], ct);
        finalOrder!.Status.Should().Be(OrderStatus.Paid);
    }
}
