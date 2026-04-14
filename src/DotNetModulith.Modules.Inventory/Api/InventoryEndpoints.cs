using System.Diagnostics;
using DotNetModulith.Modules.Inventory.Application.Commands.ReserveStock;
using DotNetModulith.Modules.Inventory.Domain;
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
            IStockRepository repository,
            CancellationToken ct) =>
        {
            var stock = await repository.GetByProductIdAsync(productId, ct);
            return stock is not null
                ? Microsoft.AspNetCore.Http.Results.Ok(new { stock.ProductId, stock.ProductName, stock.AvailableQuantity, stock.ReservedQuantity })
                : Microsoft.AspNetCore.Http.Results.NotFound();
        })
        .WithName("GetStock")
        .WithSummary("查询库存")
        .WithDescription("根据产品ID查询指定产品的库存信息")
        .WithTags("库存");

        group.MapPost("/stocks", async (
            CreateStockRequest request,
            IStockRepository repository,
            CancellationToken ct) =>
        {
            var stock = Stock.Create(request.ProductId, request.ProductName, request.InitialQuantity);
            await repository.AddAsync(stock, ct);
            return Microsoft.AspNetCore.Http.Results.Created($"/api/inventory/stocks/{stock.ProductId}", new { stock.Id, stock.ProductId });
        })
        .WithName("CreateStock")
        .WithSummary("创建库存记录")
        .WithDescription("为指定产品创建新的库存记录，并设置初始库存数量")
        .WithTags("库存");

        group.MapPost("/stocks/{productId}/replenish", async (
            string productId,
            ReplenishStockRequest request,
            IStockRepository repository,
            CancellationToken ct) =>
        {
            var stock = await repository.GetByProductIdAsync(productId, ct);
            if (stock is null)
                return Microsoft.AspNetCore.Http.Results.NotFound();

            stock.Replenish(request.Quantity);
            await repository.UpdateAsync(stock, ct);
            return Microsoft.AspNetCore.Http.Results.NoContent();
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
