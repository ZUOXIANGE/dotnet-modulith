using System.ComponentModel;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Orders.Application.Commands.ConfirmOrder;
using DotNetModulith.Modules.Orders.Application.Commands.CreateOrder;
using DotNetModulith.Modules.Orders.Application.Queries.GetOrder;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Mcp.Contracts;
using Mediator;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DotNetModulith.Modules.Orders.Mcp.Tools;

[McpServerToolType]
public sealed class OrdersMcpTools
{
    private readonly IMediator _mediator;
    private readonly OrdersMcpAuthorizationService _authorizationService;
    private readonly ILogger<OrdersMcpTools> _logger;

    public OrdersMcpTools(
        IMediator mediator,
        OrdersMcpAuthorizationService authorizationService,
        ILogger<OrdersMcpTools> logger)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [McpServerTool(Name = "create_order"), Description("创建订单。需要 orders.manage 权限。")]
    public async Task<OrderToolResult> CreateOrder(
        CreateOrderToolRequest request,
        CancellationToken cancellationToken)
    {
        await _authorizationService.EnsureManagePermissionAsync(cancellationToken);

        try
        {
            var orderId = await _mediator.Send(OrdersMcpMapper.ToCommand(request), cancellationToken);
            return new OrderToolResult(orderId.ToString(), "Pending", "Order created successfully.");
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP create_order failed for customer {CustomerId}", request.CustomerId);
            throw;
        }
    }

    [McpServerTool(Name = "confirm_order"), Description("确认订单。需要 orders.manage 权限。")]
    public async Task<OrderToolResult> ConfirmOrder(
        ConfirmOrderToolRequest request,
        CancellationToken cancellationToken)
    {
        await _authorizationService.EnsureManagePermissionAsync(cancellationToken);

        try
        {
            await _mediator.Send(new ConfirmOrderCommand(new OrderId(request.OrderId)), cancellationToken);
            return new OrderToolResult(request.OrderId.ToString(), "Confirmed", "Order confirmed successfully.");
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP confirm_order failed for order {OrderId}", request.OrderId);
            throw;
        }
    }

    [McpServerTool(Name = "get_order"), Description("查询订单详情。需要 orders.view 权限。")]
    public async Task<OrderDetailToolResult> GetOrder(
        GetOrderToolRequest request,
        CancellationToken cancellationToken)
    {
        await _authorizationService.EnsureViewPermissionAsync(cancellationToken);

        try
        {
            var detail = await _mediator.Send(new GetOrderQuery(new OrderId(request.OrderId)), cancellationToken);
            if (detail is null)
            {
                throw new BusinessException(
                    $"Order {request.OrderId} not found.",
                    ApiCodes.Common.NotFound,
                    404);
            }

            return OrdersMcpMapper.ToToolResult(detail);
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP get_order failed for order {OrderId}", request.OrderId);
            throw;
        }
    }
}