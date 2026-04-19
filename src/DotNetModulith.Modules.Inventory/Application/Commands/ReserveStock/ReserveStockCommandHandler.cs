using System.Diagnostics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Infrastructure;
using Mediator;

namespace DotNetModulith.Modules.Inventory.Application.Commands.ReserveStock;

/// <summary>
/// 预留库存命令处理器
/// </summary>
public sealed class ReserveStockCommandHandler : ICommandHandler<ReserveStockCommand, Result>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Inventory");

    private readonly IStockRepository _stockRepository;
    private readonly InventoryDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;

    public ReserveStockCommandHandler(
        IStockRepository stockRepository,
        InventoryDbContext dbContext,
        ICapPublisher capPublisher)
    {
        _stockRepository = stockRepository;
        _dbContext = dbContext;
        _capPublisher = capPublisher;
    }

    public async ValueTask<Result> Handle(ReserveStockCommand command, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("ReserveStock", ActivityKind.Internal);
        activity?.SetTag("modulith.order_id", command.OrderId);

        Result result = Result.Success();

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async ct =>
            {
                var requestedQuantities = command.Lines
                    .GroupBy(line => line.ProductId, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity), StringComparer.Ordinal);

                var stocksToReserve = new List<(Stock Stock, int Quantity)>();

                foreach (var request in requestedQuantities)
                {
                    var stock = await _stockRepository.GetByProductIdAsync(request.Key, ct);

                    if (stock is null)
                    {
                        result = Result.Failure($"Stock for product {request.Key} not found.", "STOCK_NOT_FOUND");
                        return;
                    }

                    if (stock.AvailableQuantity < request.Value)
                    {
                        result = Result.Failure($"Insufficient stock for product {request.Key}.", "INSUFFICIENT_STOCK");
                        return;
                    }

                    stocksToReserve.Add((stock, request.Value));
                }

                foreach (var (stock, quantity) in stocksToReserve)
                {
                    stock.TryReserve(quantity);
                    await _stockRepository.UpdateAsync(stock, ct);
                }
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result;
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        return result;
    }
}
