using System.Diagnostics;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Inventory.Domain;
using Mediator;

namespace DotNetModulith.Modules.Inventory.Application.Commands.ReplenishStock;

/// <summary>
/// 补充库存命令
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="Quantity">补充数量</param>
public sealed record ReplenishStockCommand(string ProductId, int Quantity) : ICommand<Result>;

/// <summary>
/// 补充库存命令处理器
/// </summary>
public sealed class ReplenishStockCommandHandler : ICommandHandler<ReplenishStockCommand, Result>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Inventory");

    private readonly IStockRepository _stockRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public ReplenishStockCommandHandler(
        IStockRepository stockRepository,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _stockRepository = stockRepository;
        _domainEventDispatcher = domainEventDispatcher;
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

        stock.Replenish(command.Quantity);
        await _stockRepository.UpdateAsync(stock, cancellationToken);
        await _domainEventDispatcher.DispatchAsync(stock, cancellationToken);

        activity?.SetStatus(ActivityStatusCode.Ok);

        return Result.Success();
    }
}
