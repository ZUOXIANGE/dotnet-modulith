using System.Diagnostics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Inventory.Api;
using DotNetModulith.Modules.Inventory.Application;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DotNetModulith.Modules.Inventory.Application.Services;

internal sealed class InventoryService : IInventoryService
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Inventory");

    private readonly IStockRepository _stockRepository;
    private readonly InventoryDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;

    public InventoryService(
        IStockRepository stockRepository,
        InventoryDbContext dbContext,
        ICapPublisher capPublisher)
    {
        _stockRepository = stockRepository;
        _dbContext = dbContext;
        _capPublisher = capPublisher;
    }

    public async Task<Result> CheckStockAsync(IReadOnlyList<CheckStockLine> lines, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("CheckStock", ActivityKind.Internal);
        activity?.SetTag("modulith.line_count", lines.Count);

        var aggregated = lines
            .GroupBy(line => line.ProductId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity), StringComparer.Ordinal);

        foreach (var (productId, requestedQuantity) in aggregated)
        {
            var stock = await _stockRepository.GetByProductIdAsync(productId, ct);

            if (stock is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, $"Stock for product {productId} not found");
                return Result.Failure(
                    $"Stock for product {productId} not found.",
                    ApiCodes.Inventory.InsufficientStock.ToString());
            }

            if (stock.AvailableQuantity < requestedQuantity)
            {
                activity?.SetStatus(ActivityStatusCode.Error, $"Insufficient stock for product {productId}");
                return Result.Failure(
                    $"Insufficient stock for product {productId}: requested {requestedQuantity}, available {stock.AvailableQuantity}.",
                    ApiCodes.Inventory.InsufficientStock.ToString());
            }
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        return Result.Success();
    }

    public async Task<Result> ReserveStockAsync(
        string orderId,
        string customerId,
        decimal totalAmount,
        IReadOnlyList<ReserveStockLine> lines,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("ReserveStockSync", ActivityKind.Internal);
        activity?.SetTag("modulith.order_id", orderId);
        activity?.SetTag("modulith.source", "module-api");

        Result result = Result.Success();
        List<StockReservedLine> reservedLines = [];

        try
        {
            await CapTransactionScope.ExecuteAsync(
                _dbContext,
                _capPublisher,
                async innerCt =>
                {
                    var requestedQuantities = lines
                        .GroupBy(line => line.ProductId, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity), StringComparer.Ordinal);

                    var stocksToReserve = new List<(StockEntity Stock, int Quantity)>();

                    foreach (var request in requestedQuantities)
                    {
                        var stock = await _stockRepository.GetByProductIdAsync(request.Key, innerCt);

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
                        stock.AvailableQuantity -= quantity;
                        stock.ReservedQuantity += quantity;
                        stock.UpdatedAt = DateTimeOffset.UtcNow;
                        await _stockRepository.UpdateAsync(stock, innerCt);

                        var reservation = new StockReservationEntity
                        {
                            Id = Guid.NewGuid(),
                            StockId = stock.Id,
                            OrderId = orderId,
                            ProductId = stock.ProductId,
                            Quantity = quantity,
                            Status = StockReservationStatus.Pending,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        await _stockRepository.AddReservationAsync(reservation, innerCt);
                        reservedLines.Add(new StockReservedLine(stock.ProductId, quantity));
                    }

                    var reservedEvent = new StockReservedIntegrationEvent(
                        orderId,
                        customerId,
                        totalAmount,
                        reservedLines);

                    await _capPublisher.PublishAsync(
                        "modulith.inventory.StockReservedIntegrationEvent",
                        reservedEvent,
                        cancellationToken: innerCt);
                },
            ct);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex))
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Result.Success();
        }

        if (!result.IsSuccess)
        {
            return result;
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        return result;
    }

    private static bool IsDuplicateKeyViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
