using DotNetModulith.Modules.Orders.Domain;
using Mediator;

namespace DotNetModulith.Modules.Orders.Application.Commands.CreateOrder;

/// <summary>
/// 创建订单命令
/// </summary>
/// <param name="CustomerId">客户ID</param>
/// <param name="Lines">订单行项目数据列表</param>
public sealed record CreateOrderCommand(
    string CustomerId,
    IReadOnlyList<OrderLineData> Lines) : ICommand<OrderId>;
