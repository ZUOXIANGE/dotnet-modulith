using DotNetModulith.Abstractions.Contracts.Orders;
using DotNetModulith.Modules.Orders.Api.Contracts.Requests;
using DotNetModulith.Modules.Orders.Application.Commands.CreateOrder;
using DotNetModulith.Modules.Orders.Application.Queries.GetOrder;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Domain.Events;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Orders.Application.Mappings;

/// <summary>
/// 订单模块对象映射器，使用Mapperly源生成器实现编译时映射代码生成
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class OrderMapper
{
    /// <summary>
    /// 将订单聚合根映射为订单详情DTO
    /// </summary>
    [MapProperty(nameof(Order.Id), nameof(OrderDetail.OrderId))]
    [MapProperty(nameof(Order.Status), nameof(OrderDetail.Status))]
    public static partial OrderDetail ToDetail(this Order order);

    /// <summary>
    /// 将订单行项目映射为订单行详情DTO
    /// </summary>
    private static partial OrderLineDetail MapToDetail(OrderLine line);

    /// <summary>
    /// 将订单ID转换为字符串
    /// </summary>
    private static string MapOrderIdToString(OrderId id) => id.ToString();

    /// <summary>
    /// 将订单状态枚举转换为字符串
    /// </summary>
    private static string MapStatusToString(OrderStatus status) => status.ToString();

    /// <summary>
    /// 将订单行项目数据映射为订单行契约
    /// </summary>
    public static partial OrderLineContract ToContract(this OrderLineData line);

    /// <summary>
    /// 将订单行项目数据列表映射为订单行契约列表
    /// </summary>
    public static partial IReadOnlyList<OrderLineContract> ToContractList(this IReadOnlyList<OrderLineData> lines);

    /// <summary>
    /// 将创建订单请求映射为创建订单命令
    /// </summary>
    public static partial CreateOrderCommand ToCommand(this CreateOrderRequest request);

    /// <summary>
    /// 将创建订单行项目请求映射为订单行项目数据
    /// </summary>
    private static partial OrderLineData MapToOrderLineData(CreateOrderLineRequest request);

    /// <summary>
    /// 将订单创建领域事件映射为订单创建集成事件
    /// </summary>
    public static OrderCreatedIntegrationEvent ToIntegrationEvent(
        this OrderCreatedDomainEvent domainEvent) => new(
        domainEvent.OrderId.ToString(),
        domainEvent.CustomerId,
        domainEvent.TotalAmount,
        domainEvent.Lines.ToContractList());

    /// <summary>
    /// 将订单支付领域事件映射为订单支付集成事件
    /// </summary>
    public static OrderPaidIntegrationEvent ToIntegrationEvent(
        this OrderPaidDomainEvent domainEvent) => new(
        domainEvent.OrderId.ToString(),
        domainEvent.CustomerId,
        domainEvent.TotalAmount);

    /// <summary>
    /// 将订单取消领域事件映射为订单取消集成事件
    /// </summary>
    public static OrderCancelledIntegrationEvent ToIntegrationEvent(
        this OrderCancelledDomainEvent domainEvent) => new(
        domainEvent.OrderId.ToString(),
        domainEvent.CustomerId,
        domainEvent.Reason);
}
