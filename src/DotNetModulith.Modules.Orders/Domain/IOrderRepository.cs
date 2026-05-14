namespace DotNetModulith.Modules.Orders.Domain;

/// <summary>
/// 订单仓储接口，提供订单的持久化操作
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// 根据订单ID获取订单
    /// </summary>
    /// <param name="id">订单ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>订单实体，未找到时返回null</returns>
    Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 根据客户ID获取订单列表
    /// </summary>
    /// <param name="customerId">客户ID</param>
    /// <param name="limit">最大返回数量</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>订单实体列表</returns>
    Task<IReadOnlyList<OrderEntity>> GetByCustomerIdAsync(string customerId, int limit, CancellationToken ct = default);

    /// <summary>
    /// 获取待处理订单列表
    /// </summary>
    /// <param name="limit">最大返回数量</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>待处理订单实体列表</returns>
    Task<IReadOnlyList<OrderEntity>> GetPendingOrdersAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// 添加新订单
    /// </summary>
    /// <param name="order">要添加的订单</param>
    /// <param name="ct">取消令牌</param>
    Task AddAsync(OrderEntity order, CancellationToken ct = default);

    /// <summary>
    /// 更新已有订单
    /// </summary>
    /// <param name="order">要更新的订单</param>
    /// <param name="ct">取消令牌</param>
    Task UpdateAsync(OrderEntity order, CancellationToken ct = default);

    /// <summary>
    /// 提交当前工作单元中的订单变更
    /// </summary>
    /// <param name="ct">取消令牌</param>
    Task SaveChangesAsync(CancellationToken ct = default);
}
