using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Orders.Domain;
using Mediator;

namespace DotNetModulith.Modules.Orders.Application.Commands.ConfirmOrder;

/// <summary>
/// 确认订单命令
/// </summary>
/// <param name="OrderId">订单ID</param>
public sealed record ConfirmOrderCommand(OrderId OrderId) : ICommand<Result>;
