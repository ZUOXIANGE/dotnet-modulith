namespace DotNetModulith.Modules.Inventory.Domain;

/// <summary>
/// 库存仓储接口，提供库存的持久化操作
/// </summary>
public interface IStockRepository
{
    /// <summary>
    /// 根据库存ID获取库存
    /// </summary>
    /// <param name="id">库存ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>库存实体，未找到时返回null</returns>
    Task<StockEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 根据产品ID获取库存
    /// </summary>
    /// <param name="productId">产品ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>库存实体，未找到时返回null</returns>
    Task<StockEntity?> GetByProductIdAsync(string productId, CancellationToken ct = default);

    /// <summary>
    /// 获取低库存商品列表
    /// </summary>
    /// <param name="threshold">库存阈值</param>
    /// <param name="limit">最大返回数量</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>低库存实体列表</returns>
    Task<IReadOnlyList<StockEntity>> GetLowStockAsync(int threshold, int limit, CancellationToken ct = default);

    /// <summary>
    /// 根据订单ID获取库存预留记录
    /// </summary>
    /// <param name="orderId">订单ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>库存预留实体列表</returns>
    Task<IReadOnlyList<StockReservationEntity>> GetReservationsByOrderIdAsync(string orderId, CancellationToken ct = default);

    /// <summary>
    /// 添加新库存
    /// </summary>
    /// <param name="stock">要添加的库存</param>
    /// <param name="ct">取消令牌</param>
    Task AddAsync(StockEntity stock, CancellationToken ct = default);

    /// <summary>
    /// 更新已有库存
    /// </summary>
    /// <param name="stock">要更新的库存</param>
    /// <param name="ct">取消令牌</param>
    Task UpdateAsync(StockEntity stock, CancellationToken ct = default);

    /// <summary>
    /// 添加库存预留记录
    /// </summary>
    /// <param name="reservation">要添加的预留记录</param>
    /// <param name="ct">取消令牌</param>
    Task AddReservationAsync(StockReservationEntity reservation, CancellationToken ct = default);

    /// <summary>
    /// 更新库存预留记录
    /// </summary>
    /// <param name="reservation">要更新的预留记录</param>
    /// <param name="ct">取消令牌</param>
    Task UpdateReservationAsync(StockReservationEntity reservation, CancellationToken ct = default);

    /// <summary>
    /// 提交当前工作单元中的库存变更
    /// </summary>
    /// <param name="ct">取消令牌</param>
    Task SaveChangesAsync(CancellationToken ct = default);
}
