using DotNetModulith.Modules.Inventory.Application.Mappings;
using DotNetModulith.Modules.Inventory.Domain;
using Mediator;

namespace DotNetModulith.Modules.Inventory.Application.Queries.GetStock;

/// <summary>
/// 根据产品ID查询库存
/// </summary>
/// <param name="ProductId">产品ID</param>
public sealed record GetStockByProductIdQuery(string ProductId) : IQuery<StockDetail?>;

/// <summary>
/// 根据产品ID查询库存处理器
/// </summary>
public sealed class GetStockByProductIdQueryHandler : IQueryHandler<GetStockByProductIdQuery, StockDetail?>
{
    private readonly IStockRepository _stockRepository;

    public GetStockByProductIdQueryHandler(IStockRepository stockRepository)
    {
        _stockRepository = stockRepository;
    }

    public async ValueTask<StockDetail?> Handle(GetStockByProductIdQuery query, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetByProductIdAsync(query.ProductId, cancellationToken);
        return stock?.ToDetail();
    }
}
