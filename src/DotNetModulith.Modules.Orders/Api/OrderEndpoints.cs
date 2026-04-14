using System.Diagnostics;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Orders.Application.Commands.ConfirmOrder;
using DotNetModulith.Modules.Orders.Application.Commands.CreateOrder;
using DotNetModulith.Modules.Orders.Application.Mappings;
using DotNetModulith.Modules.Orders.Application.Queries.GetOrder;
using DotNetModulith.Modules.Orders.Domain;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Orders.Api;

public static class OrderEndpoints
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Orders");

    public static WebApplication MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders");

        group.MapPost("/", async (
            CreateOrderRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = request.ToCommand();

            var orderId = await mediator.Send(command, ct);
            return Microsoft.AspNetCore.Http.Results.Created($"/api/orders/{orderId}", new { orderId = orderId.ToString() });
        })
        .WithName("CreateOrder")
        .WithSummary("创建订单")
        .WithDescription("根据指定的客户和订单行项目创建新订单")
        .WithTags("订单");

        group.MapPost("/{orderId:guid}/confirm", async (
            Guid orderId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ConfirmOrderCommand(new OrderId(orderId));
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Microsoft.AspNetCore.Http.Results.NoContent()
                : Microsoft.AspNetCore.Http.Results.BadRequest(new { error = result.Error, code = result.ErrorCode });
        })
        .WithName("ConfirmOrder")
        .WithSummary("确认订单")
        .WithDescription("根据订单ID确认已有订单，确认后将触发支付和库存处理流程")
        .WithTags("订单");

        group.MapGet("/{orderId:guid}", async (
            Guid orderId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetOrderQuery(new OrderId(orderId));
            var order = await mediator.Send(query, ct);

            return order is not null
                ? Microsoft.AspNetCore.Http.Results.Ok(order)
                : Microsoft.AspNetCore.Http.Results.NotFound();
        })
        .WithName("GetOrder")
        .WithSummary("查询订单")
        .WithDescription("根据订单唯一标识符获取订单详情")
        .WithTags("订单");

        return app;
    }
}

/// <summary>
/// 创建订单请求
/// </summary>
/// <param name="CustomerId">客户ID</param>
/// <param name="Lines">订单行项目列表</param>
public sealed record CreateOrderRequest(
    string CustomerId,
    IReadOnlyList<CreateOrderLineRequest> Lines);

/// <summary>
/// 创建订单行项目请求
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="ProductName">产品名称</param>
/// <param name="Quantity">数量</param>
/// <param name="UnitPrice">单价</param>
public sealed record CreateOrderLineRequest(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
