using DotNetModulith.Abstractions.Contracts.Orders;
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
    [MapProperty(nameof(OrderEntity.Id), nameof(OrderDetail.OrderId))]
    [MapProperty(nameof(OrderEntity.Status), nameof(OrderDetail.Status))]
    public static partial OrderDetail ToDetail(this OrderEntity order);

    private static partial OrderLineDetail MapToDetail(OrderLineEntity line);

    private static string MapOrderIdToString(Guid id) => id.ToString();

    private static string MapStatusToString(OrderStatus status) => status.ToString();

    public static partial OrderLineContract ToContract(this OrderLineData line);

    public static partial IReadOnlyList<OrderLineContract> ToContractList(this IReadOnlyList<OrderLineData> lines);

    public static OrderCreatedIntegrationEvent ToIntegrationEvent(
        this OrderCreatedDomainEvent domainEvent) => new(
        domainEvent.OrderId.ToString(),
        domainEvent.CustomerId,
        domainEvent.TotalAmount,
        domainEvent.Lines.ToContractList());

    public static OrderPaidIntegrationEvent ToIntegrationEvent(
        this OrderPaidDomainEvent domainEvent) => new(
        domainEvent.OrderId.ToString(),
        domainEvent.CustomerId,
        domainEvent.TotalAmount);

    public static OrderCancelledIntegrationEvent ToIntegrationEvent(
        this OrderCancelledDomainEvent domainEvent) => new(
        domainEvent.OrderId.ToString(),
        domainEvent.CustomerId,
        domainEvent.Reason,
        domainEvent.Lines.ToContractList());
}
