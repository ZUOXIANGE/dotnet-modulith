using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Orders.Api.Contracts.Requests;
using DotNetModulith.Modules.Orders.Api.Contracts.Responses;
using DotNetModulith.Modules.Orders.Application.Caching;
using DotNetModulith.Modules.Orders.Application.Commands.ConfirmOrder;
using DotNetModulith.Modules.Orders.Application.Mappings;
using DotNetModulith.Modules.Orders.Application.Queries.GetOrder;
using DotNetModulith.Modules.Orders.Domain;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Orders.Api.Controllers;

/// <summary>
/// 订单管理接口
/// </summary>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFusionCache _cache;

    public OrdersController(IMediator mediator, IFusionCache cache)
    {
        _mediator = mediator;
        _cache = cache;
    }

    /// <summary>
    /// 创建订单
    /// </summary>
    /// <param name="request">创建订单请求。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>创建后的订单标识。</returns>
    [HttpPost]
    public async Task<ApiResponse<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var command = request.ToCommand();
        var orderId = await _mediator.Send(command, ct);
        return ApiResponse.Success(new CreateOrderResponse(orderId.ToString()));
    }

    /// <summary>
    /// 确认订单
    /// </summary>
    /// <param name="orderId">订单标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>统一成功响应。</returns>
    [HttpPost("{orderId:guid}/confirm")]
    public async Task<ApiResponse<object?>> ConfirmOrder(Guid orderId, CancellationToken ct)
    {
        var command = new ConfirmOrderCommand(new OrderId(orderId));
        await _mediator.Send(command, ct);
        return ApiResponse.Success();
    }

    /// <summary>
    /// 查询订单详情
    /// </summary>
    /// <param name="orderId">订单标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>订单详情。</returns>
    [HttpGet("{orderId:guid}")]
    public async Task<ApiResponse<OrderDetail>> GetOrder(Guid orderId, CancellationToken ct)
    {
        var query = new GetOrderQuery(new OrderId(orderId));
        var order = await _mediator.Send(query, ct);

        if (order is null)
        {
            throw new BusinessException(
                $"Order {orderId} not found.",
                ApiCodes.Common.NotFound,
                StatusCodes.Status404NotFound);
        }

        return ApiResponse.Success(order);
    }

    /// <summary>
    /// 手动清理订单缓存
    /// </summary>
    /// <param name="orderId">订单标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>统一成功响应。</returns>
    [HttpDelete("{orderId:guid}/cache")]
    public async Task<ApiResponse<object?>> ClearOrderCache(Guid orderId, CancellationToken ct)
    {
        await _cache.RemoveAsync(OrderCacheKeys.OrderDetail(orderId.ToString()), null, ct);
        return ApiResponse.Success();
    }
}
