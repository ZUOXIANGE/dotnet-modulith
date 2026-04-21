using System.Diagnostics;
using DotNetModulith.Abstractions.Domain;
using DotNetModulith.Modules.Orders.Domain.Events;

namespace DotNetModulith.Modules.Orders.Domain;

/// <summary>
/// 订单聚合根，管理订单的生命周期和业务规则
/// </summary>
public sealed class Order : AggregateRoot, IEntity<OrderId>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    /// <summary>
    /// 订单唯一标识
    /// </summary>
    public OrderId Id { get; private set; } = null!;

    /// <summary>
    /// 客户ID
    /// </summary>
    public string CustomerId { get; private set; } = null!;

    /// <summary>
    /// 订单行项目列表
    /// </summary>
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// 订单状态
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// 订单总金额
    /// </summary>
    public decimal TotalAmount => _lines.Sum(l => l.Quantity * l.UnitPrice);

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    private readonly List<OrderLine> _lines = [];

    private Order() { }

    /// <summary>
    /// 创建新订单
    /// </summary>
    /// <param name="customerId">客户ID</param>
    /// <param name="lines">订单行项目数据列表</param>
    /// <returns>新创建的订单实例</returns>
    public static Order Create(string customerId, IReadOnlyList<OrderLineData> lines)
    {
        using var activity = ActivitySource.StartActivity("Order.Create", ActivityKind.Internal);

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID is required.", nameof(customerId));

        if (lines.Count == 0)
            throw new ArgumentException("Order must have at least one line.", nameof(lines));

        var order = new Order
        {
            Id = OrderId.New(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var line in lines)
        {
            var orderLine = new OrderLine(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice);
            order._lines.Add(orderLine);
        }

        var domainEvent = new OrderCreatedDomainEvent(order.Id, order.CustomerId, order.TotalAmount, lines);
        order.AddDomainEvent(domainEvent);

        activity?.SetTag("modulith.order_id", order.Id.ToString());
        activity?.SetTag("modulith.customer_id", order.CustomerId);
        activity?.SetTag("modulith.total_amount", order.TotalAmount);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return order;
    }

    /// <summary>
    /// 确认订单，状态须为待确认
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm order in status {Status}.");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 标记订单为已支付，状态须为已确认
    /// </summary>
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot mark as paid order in status {Status}.");

        Status = OrderStatus.Paid;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderPaidDomainEvent(Id, CustomerId, TotalAmount));
    }

    /// <summary>
    /// 取消订单，已支付或已发货的订单不可取消
    /// </summary>
    /// <param name="reason">取消原因</param>
    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Paid)
            throw new InvalidOperationException($"Cannot cancel order in status {Status}.");

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;

        var lines = _lines
            .Select(line => new OrderLineData(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice))
            .ToList();

        AddDomainEvent(new OrderCancelledDomainEvent(Id, CustomerId, reason, lines));
    }
}
