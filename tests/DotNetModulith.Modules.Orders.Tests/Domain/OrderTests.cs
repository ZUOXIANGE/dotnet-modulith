using DotNetModulith.Modules.Orders.Domain;
using FluentAssertions;
using Xunit;

namespace DotNetModulith.Modules.Orders.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        var lines = new List<OrderLineEntity>
        {
            new("PROD-001", "Widget", 2, 10.00m),
            new("PROD-002", "Gadget", 1, 25.00m)
        };

        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = "CUST-001",
            Status = OrderStatus.Pending,
            TotalAmount = 45.00m,
            Lines = lines,
            CreatedAt = DateTimeOffset.UtcNow
        };

        order.Id.Should().NotBeEmpty();
        order.CustomerId.Should().Be("CUST-001");
        order.Status.Should().Be(OrderStatus.Pending);
        order.Lines.Should().HaveCount(2);
        order.TotalAmount.Should().Be(45.00m);
    }

    [Fact]
    public void StatusTransition_ShouldBeDirectPropertyAssignment()
    {
        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = "CUST-001",
            Status = OrderStatus.Pending,
            TotalAmount = 10m,
            CreatedAt = DateTimeOffset.UtcNow
        };

        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.UpdatedAt.Should().NotBeNull();
    }
}
