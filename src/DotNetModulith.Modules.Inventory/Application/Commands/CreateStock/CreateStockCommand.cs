using System.Diagnostics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Inventory.Domain;
using DotNetModulith.Modules.Inventory.Infrastructure;
using Mediator;

namespace DotNetModulith.Modules.Inventory.Application.Commands.CreateStock;

/// <summary>
/// 创建库存记录命令
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="ProductName">产品名称</param>
/// <param name="InitialQuantity">初始库存数量</param>
public sealed record CreateStockCommand(
    string ProductId,
    string ProductName,
    int InitialQuantity) : ICommand<StockId>;

/// <summary>
/// 创建库存记录命令处理器
/// </summary>
public sealed class CreateStockCommandHandler : ICommandHandler<CreateStockCommand, StockId>
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Inventory");

    private readonly IStockRepository _stockRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly InventoryDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;

    public CreateStockCommandHandler(
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

    public async ValueTask<StockId> Handle(CreateStockCommand command, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("CreateStock", ActivityKind.Internal);
        activity?.SetTag("modulith.product_id", command.ProductId);

        var stock = Stock.Create(command.ProductId, command.ProductName, command.InitialQuantity);

        await CapTransactionScope.ExecuteAsync(
            _dbContext,
            _capPublisher,
            async ct =>
            {
                await _stockRepository.AddAsync(stock, ct);
                await _domainEventDispatcher.DispatchAsync(stock, ct);
            },
            cancellationToken);

        activity?.SetStatus(ActivityStatusCode.Ok);

        return stock.Id;
    }
}
