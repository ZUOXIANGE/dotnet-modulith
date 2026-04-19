using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Domain.Events;
using FluentAssertions;
using Xunit;

namespace DotNetModulith.Modules.Orders.Tests.Domain;

/// <summary>
/// 订单领域实体单元测试，验证订单的创建、状态流转和业务规则
/// </summary>
public class OrderTests
{
    /// <summary>
    /// 验证使用有效数据创建订单时，应正确初始化所有属性并产生领域事件
    /// </summary>
    [Fact]
    public void Create_WithValidData_ShouldCreateOrder()
    {
        var lines = new List<OrderLineData>
        {
            new("PROD-001", "Widget", 2, 10.00m),
            new("PROD-002", "Gadget", 1, 25.00m)
        };

        var order = Order.Create("CUST-001", lines);

        order.Id.Should().NotBeNull();
        order.CustomerId.Should().Be("CUST-001");
        order.Status.Should().Be(OrderStatus.Pending);
        order.Lines.Should().HaveCount(2);
        order.TotalAmount.Should().Be(45.00m);
        order.DomainEvents.Should().ContainSingle(e => e is OrderCreatedDomainEvent);

        var createdEvent = order.DomainEvents.OfType<OrderCreatedDomainEvent>().First();
        createdEvent.Lines.Should().HaveCount(2);
        createdEvent.Lines[0].ProductId.Should().Be("PROD-001");
        createdEvent.Lines[1].ProductId.Should().Be("PROD-002");
    }

    /// <summary>
    /// 验证客户ID为空时创建订单应抛出参数异常
    /// </summary>
    [Fact]
    public void Create_WithEmptyCustomerId_ShouldThrow()
    {
        var act = () => Order.Create("", [new OrderLineData("P1", "Product", 1, 10m)]);

        act.Should().Throw<ArgumentException>().WithMessage("*Customer ID*");
    }

    /// <summary>
    /// 验证订单行项目为空时创建订单应抛出参数异常
    /// </summary>
    [Fact]
    public void Create_WithNoLines_ShouldThrow()
    {
        var act = () => Order.Create("CUST-001", []);

        act.Should().Throw<ArgumentException>().WithMessage("*at least one line*");
    }

    /// <summary>
    /// 验证待确认状态的订单可以成功确认
    /// </summary>
    [Fact]
    public void Confirm_WhenPending_ShouldSucceed()
    {
        var order = CreateTestOrder();

        order.Confirm();

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.UpdatedAt.Should().NotBeNull();
    }

    /// <summary>
    /// 验证已确认的订单再次确认应抛出无效操作异常
    /// </summary>
    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrow()
    {
        var order = CreateTestOrder();
        order.Confirm();

        var act = () => order.Confirm();

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// 验证已确认的订单可以标记为已支付，并产生支付领域事件
    /// </summary>
    [Fact]
    public void MarkAsPaid_WhenConfirmed_ShouldSucceed()
    {
        var order = CreateTestOrder();
        order.Confirm();

        order.MarkAsPaid();

        order.Status.Should().Be(OrderStatus.Paid);
        order.DomainEvents.Should().ContainSingle(e => e is OrderPaidDomainEvent);
    }

    /// <summary>
    /// 验证待确认状态的订单可以取消，并产生取消领域事件
    /// </summary>
    [Fact]
    public void Cancel_WhenPending_ShouldSucceed()
    {
        var order = CreateTestOrder();

        order.Cancel("Customer request");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledDomainEvent);
        var cancelledEvent = order.DomainEvents.OfType<OrderCancelledDomainEvent>().Single();
        cancelledEvent.Lines.Should().ContainSingle();
        cancelledEvent.Lines[0].ProductId.Should().Be("PROD-001");
        cancelledEvent.Lines[0].Quantity.Should().Be(2);
    }

    /// <summary>
    /// 验证已支付的订单不可取消
    /// </summary>
    [Fact]
    public void Cancel_WhenPaid_ShouldThrow()
    {
        var order = CreateTestOrder();
        order.Confirm();
        order.MarkAsPaid();

        var act = () => order.Cancel("Too late");

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// 验证清除领域事件后事件列表应为空
    /// </summary>
    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        var order = CreateTestOrder();
        order.DomainEvents.Should().NotBeEmpty();

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// 创建测试用的订单实例
    /// </summary>
    private static Order CreateTestOrder() =>
        Order.Create("CUST-001", [new OrderLineData("PROD-001", "Widget", 2, 10.00m)]);
}
