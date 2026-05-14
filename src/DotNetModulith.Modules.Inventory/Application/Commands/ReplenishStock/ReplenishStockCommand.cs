using System.Diagnostics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Domain.Events;
using DotNetModulith.Modules.Inventory.Infrastructure;
using Mediator;

namespace DotNetModulith.Modules.Inventory.Application.Commands.ReplenishStock;

/// <summary>
/// 补货命令
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="Quantity">补货数量</param>
public sealed record ReplenishStockCommand(string ProductId, int Quantity) : ICommand<Result>;

/// <summary>
/// 补货命令处理器
/// </summary>
public sealed class ReplenishStockCommandHandler : ICommandHandler<ReplenishStockCommand, Result>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Inventory");

    private readonly IStockRepository _stockRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly InventoryDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;

    public ReplenishStockCommandHandler(
        IStockRepository stockRepository,
        IDomainEventDispatcher domainEventDispatcher,
        InventoryDbContext dbContext,
        ICapPublisher capPublisher)
    {
        _stockRepository = stockRepository;
        _domainEventDispatcher = domainEventDispatcher;
        _dbContext = dbContext;
        _capPublisher = capPublisher;
    }

    public async ValueTask<Result> Handle(ReplenishStockCommand command, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("ReplenishStock", ActivityKind.Internal);
        activity?.SetTag("modulith.product_id", command.ProductId);

        var stock = await _stockRepository.GetByProductIdAsync(command.ProductId, cancellationToken);

        if (stock is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Stock not found");
            throw new BusinessException(
                message: $"Stock for product {command.ProductId} not found.",
                code: ApiCodes.Common.NotFound,
                httpStatusCode: 404);
        }

        stock.AvailableQuantity += command.Quantity;
        stock.UpdatedAt = DateTimeOffset.UtcNow;
        stock.LowStockAlertSentAt = null;
        stock.LastAlertedAvailableQuantity = null;

        var domainEvent = new StockReplenishedDomainEvent(stock.Id, stock.ProductId, command.Quantity);

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async ct =>
            {
                await _stockRepository.UpdateAsync(stock, ct);
                await _domainEventDispatcher.DispatchAsync([domainEvent], ct);
            },
            cancellationToken);

        activity?.SetStatus(ActivityStatusCode.Ok);

        return Result.Success();
    }
}
