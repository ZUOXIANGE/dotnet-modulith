using DotNetModulith.Abstractions.Results;
using Mediator;

namespace DotNetModulith.Modules.Orders.Application.Commands.ConfirmOrder;

/// <summary>
/// 确认订单命令
/// </summary>
/// <param name="OrderId">订单ID</param>
public sealed record ConfirmOrderCommand(Guid OrderId) : ICommand<Result>;
