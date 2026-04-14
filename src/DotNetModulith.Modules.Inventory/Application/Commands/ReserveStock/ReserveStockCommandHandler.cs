using System.Diagnostics;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Inventory.Domain;
using Mediator;

namespace DotNetModulith.Modules.Inventory.Application.Commands.ReserveStock;

/// <summary>
/// 预留库存命令处理器
/// </summary>
public sealed class ReserveStockCommandHandler : ICommandHandler<ReserveStockCommand, Result>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Inventory");

    private readonly IStockRepository _stockRepository;

    public ReserveStockCommandHandler(IStockRepository stockRepository)
    {
        _stockRepository = stockRepository;
    }

    public async ValueTask<Result> Handle(ReserveStockCommand command, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("ReserveStock", ActivityKind.Internal);
        activity?.SetTag("modulith.order_id", command.OrderId);

        foreach (var line in command.Lines)
        {
            var stock = await _stockRepository.GetByProductIdAsync(line.ProductId, cancellationToken);

            if (stock is null)
                return Result.Failure($"Stock for product {line.ProductId} not found.", "STOCK_NOT_FOUND");

            if (!stock.TryReserve(line.Quantity))
                return Result.Failure($"Insufficient stock for product {line.ProductId}.", "INSUFFICIENT_STOCK");

            await _stockRepository.UpdateAsync(stock, cancellationToken);
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        return Result.Success();
    }
}
