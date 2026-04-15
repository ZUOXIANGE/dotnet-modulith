using System.Diagnostics;
using DotNetModulith.Modules.Inventory.Application.Commands.CreateStock;
using DotNetModulith.Modules.Inventory.Application.Commands.ReplenishStock;
using DotNetModulith.Modules.Inventory.Application.Commands.ReserveStock;
using DotNetModulith.Modules.Inventory.Application.Mappings;
using DotNetModulith.Modules.Inventory.Application.Queries.GetStock;
using DotNetModulith.Modules.Inventory.Domain;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Inventory.Api;

public static class InventoryEndpoints
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Inventory");

    public static WebApplication MapInventoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/inventory");

        group.MapGet("/stocks/{productId}", async (
            string productId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetStockByProductIdQuery(productId);
            var stock = await mediator.Send(query, ct);

            return stock is not null
                ? Microsoft.AspNetCore.Http.Results.Ok(stock)
                : Microsoft.AspNetCore.Http.Results.NotFound();
        })
        .WithName("GetStock")
        .WithSummary("查询库存")
        .WithDescription("根据产品ID查询指定产品的库存信息")
        .WithTags("库存");

        group.MapPost("/stocks", async (
            CreateStockRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new CreateStockCommand(request.ProductId, request.ProductName, request.InitialQuantity);
            var stockId = await mediator.Send(command, ct);
            return Microsoft.AspNetCore.Http.Results.Created($"/api/inventory/stocks/{request.ProductId}", new { StockId = stockId.ToString() });
        })
        .WithName("CreateStock")
        .WithSummary("创建库存记录")
        .WithDescription("为指定产品创建新的库存记录，并设置初始库存数量")
        .WithTags("库存");

        group.MapPost("/stocks/{productId}/replenish", async (
            string productId,
            ReplenishStockRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ReplenishStockCommand(productId, request.Quantity);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Microsoft.AspNetCore.Http.Results.NoContent()
                : Microsoft.AspNetCore.Http.Results.BadRequest(new { error = result.Error, code = result.ErrorCode });
        })
        .WithName("ReplenishStock")
        .WithSummary("补充库存")
        .WithDescription("为指定产品补充库存数量")
        .WithTags("库存");

        return app;
    }
}

/// <summary>
/// 创建库存记录请求
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="ProductName">产品名称</param>
/// <param name="InitialQuantity">初始库存数量</param>
public sealed record CreateStockRequest(string ProductId, string ProductName, int InitialQuantity);

/// <summary>
/// 补充库存请求
/// </summary>
/// <param name="Quantity">补充数量</param>
public sealed record ReplenishStockRequest(int Quantity);
