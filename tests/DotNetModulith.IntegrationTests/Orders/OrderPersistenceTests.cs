using DotNetModulith.IntegrationTests.Fixtures;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetModulith.IntegrationTests.Orders;

/// <summary>
/// 订单持久化集成测试，验证订单的数据库读写和状态流转
/// </summary>
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

    /// <summary>
    /// 验证保存订单后能从数据库中正确读取
    /// </summary>
    [Fact]
    public async Task SaveOrder_ShouldPersistToDatabase()
    {
        var ct = TestContext.Current.CancellationToken;

        await dbFixture.ResetAsync();

        var order = OrderEntity.Create("CUST-001",
        [
            new OrderLineData("PROD-001", "Widget", 2, 10.00m),
            new OrderLineData("PROD-002", "Gadget", 1, 25.00m)
        ]);

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        var saved = await _dbContext.Orders
            .Include("_lines")
            .FirstOrDefaultAsync(o => o.Id == order.Id, ct);

        saved.Should().NotBeNull();
        saved!.CustomerId.Should().Be("CUST-001");
        saved.Status.Should().Be(OrderStatus.Pending);
    }

    /// <summary>
    /// 验证订单状态流转（待确认→已确认→已支付）能正确持久化
    /// </summary>
    [Fact]
    public async Task OrderStatusTransition_ShouldPersist()
    {
        var ct = TestContext.Current.CancellationToken;

        await dbFixture.ResetAsync();

        var order = OrderEntity.Create("CUST-002", [new OrderLineData("PROD-003", "Bolt", 5, 3.50m)]);

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        order.Confirm();
        order.MarkAsPaid();

        _dbContext.Orders.Update(order);
        await _dbContext.SaveChangesAsync(ct);

        var saved = await _dbContext.Orders
            .Include("_lines")
            .FirstOrDefaultAsync(o => o.Id == order.Id, ct);

        saved.Should().NotBeNull();
        saved!.Status.Should().Be(OrderStatus.Paid);
    }
}

/// <summary>
/// 数据库测试集合定义，共享PostgreSQL容器实例
/// </summary>
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlFixture>;

