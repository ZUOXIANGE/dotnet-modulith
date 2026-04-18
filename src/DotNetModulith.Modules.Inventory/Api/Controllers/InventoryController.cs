using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Inventory.Api.Contracts.Requests;
using DotNetModulith.Modules.Inventory.Api.Contracts.Responses;
using DotNetModulith.Modules.Inventory.Application.Commands.CreateStock;
using DotNetModulith.Modules.Inventory.Application.Commands.ReplenishStock;
using DotNetModulith.Modules.Inventory.Application.Mappings;
using DotNetModulith.Modules.Inventory.Application.Queries.GetStock;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Inventory.Api.Controllers;

/// <summary>
/// 库存管理接口
/// </summary>
[ApiController]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 查询指定产品库存
    /// </summary>
    /// <param name="productId">产品标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>库存详情。</returns>
    [HttpGet("stocks/{productId}")]
    public async Task<ApiResponse<StockDetail>> GetStock(string productId, CancellationToken ct)
    {
        var query = new GetStockByProductIdQuery(productId);
        var stock = await _mediator.Send(query, ct);

        if (stock is null)
        {
            throw new BusinessException(
                $"Stock for product {productId} not found.",
                ApiCodes.Common.NotFound,
                StatusCodes.Status404NotFound);
        }

        return ApiResponse.Success(stock);
    }

    /// <summary>
    /// 创建库存记录
    /// </summary>
    /// <param name="request">创建库存请求。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>创建后的库存标识。</returns>
    [HttpPost("stocks")]
    public async Task<ApiResponse<CreateStockResponse>> CreateStock([FromBody] CreateStockRequest request, CancellationToken ct)
    {
        var command = new CreateStockCommand(request.ProductId, request.ProductName, request.InitialQuantity);
        var stockId = await _mediator.Send(command, ct);
        return ApiResponse.Success(new CreateStockResponse(stockId.ToString()));
    }

    /// <summary>
    /// 补充库存
    /// </summary>
    /// <param name="productId">产品标识。</param>
    /// <param name="request">补充库存请求。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>统一成功响应。</returns>
    [HttpPost("stocks/{productId}/replenish")]
    public async Task<ApiResponse<object?>> ReplenishStock(string productId, [FromBody] ReplenishStockRequest request, CancellationToken ct)
    {
        var command = new ReplenishStockCommand(productId, request.Quantity);
        await _mediator.Send(command, ct);
        return ApiResponse.Success();
    }
}
