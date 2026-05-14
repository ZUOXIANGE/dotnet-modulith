using DotNetModulith.IntegrationTests.Fixtures;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetModulith.IntegrationTests.Orders;

[Collection("Database collection")]
public class OrderPersistenceTests(PostgreSqlFixture dbFixture) : IAsyncLifetime
{
    private OrdersDbContext _dbContext = null!;

    public async ValueTask InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql(dbFixture.ConnectionString)
            .Options;

        _dbContext = new OrdersDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task SaveOrder_ShouldPersistToDatabase()
    {
        var ct = TestContext.Current.CancellationToken;

        await dbFixture.ResetAsync();

        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = "CUST-001",
            Status = OrderStatus.Pending,
            TotalAmount = 45.00m,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines =
            [
                new OrderLineEntity("PROD-001", "Widget", 2, 10.00m),
                new OrderLineEntity("PROD-002", "Gadget", 1, 25.00m)
            ]
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        var saved = await _dbContext.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == order.Id, ct);

        saved.Should().NotBeNull();
        saved!.CustomerId.Should().Be("CUST-001");
        saved.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task OrderStatusTransition_ShouldPersist()
    {
        var ct = TestContext.Current.CancellationToken;

        await dbFixture.ResetAsync();

        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = "CUST-002",
            Status = OrderStatus.Pending,
            TotalAmount = 17.50m,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines =
            [
                new OrderLineEntity("PROD-003", "Bolt", 5, 3.50m)
            ]
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Orders.Update(order);
        await _dbContext.SaveChangesAsync(ct);

        var saved = await _dbContext.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == order.Id, ct);

        saved.Should().NotBeNull();
        saved!.Status.Should().Be(OrderStatus.Paid);
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlFixture>;
