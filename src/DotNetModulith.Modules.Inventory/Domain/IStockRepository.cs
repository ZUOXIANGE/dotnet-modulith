namespace DotNetModulith.Modules.Inventory.Domain;

/// <summary>
/// 库存仓储接口，提供库存的持久化操作
/// </summary>
public interface IStockRepository
{
    /// <summary>
    /// 根据库存ID获取库存记录（用于写操作，需要跟踪）
    /// </summary>
    /// <param name="id">库存ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>库存实例，未找到时返回null</returns>
    Task<Stock?> GetByIdAsync(StockId id, CancellationToken ct = default);

    /// <summary>
    /// 根据产品ID获取库存记录（用于写操作，需要跟踪）
    /// </summary>
    /// <param name="productId">产品ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>库存实例，未找到时返回null</returns>
    Task<Stock?> GetByProductIdAsync(string productId, CancellationToken ct = default);

    /// <summary>
    /// 获取低库存记录列表
    /// </summary>
    /// <param name="threshold">低库存阈值</param>
    /// <param name="limit">返回行数上限</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>可用数量低于阈值的库存列表</returns>
    Task<IReadOnlyList<Stock>> GetLowStockAsync(int threshold, int limit, CancellationToken ct = default);

    /// <summary>
    /// 添加新库存记录
    /// </summary>
    /// <param name="stock">要添加的库存</param>
    /// <param name="ct">取消令牌</param>
    Task AddAsync(Stock stock, CancellationToken ct = default);

    /// <summary>
    /// 更新已有库存记录
    /// </summary>
    /// <param name="stock">要更新的库存</param>
    /// <param name="ct">取消令牌</param>
    Task UpdateAsync(Stock stock, CancellationToken ct = default);
}
