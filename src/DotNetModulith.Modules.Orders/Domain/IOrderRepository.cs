using DotNetModulith.Modules.Orders.Domain;

namespace DotNetModulith.Modules.Orders.Domain;

/// <summary>
/// 订单仓储接口，提供订单的持久化操作
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// 根据订单ID获取订单（用于写操作，需要跟踪）
    /// </summary>
    /// <param name="id">订单ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>订单实例，未找到时返回null</returns>
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);

    /// <summary>
    /// 根据客户ID获取订单列表
    /// </summary>
    /// <param name="customerId">客户ID</param>
    /// <param name="limit">返回行数上限</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>该客户的订单列表</returns>
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(string customerId, int limit, CancellationToken ct = default);

    /// <summary>
    /// 获取所有待确认的订单
    /// </summary>
    /// <param name="limit">返回行数上限</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>待确认订单列表</returns>
    Task<IReadOnlyList<Order>> GetPendingOrdersAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// 添加新订单
    /// </summary>
    /// <param name="order">要添加的订单</param>
    /// <param name="ct">取消令牌</param>
    Task AddAsync(Order order, CancellationToken ct = default);

    /// <summary>
    /// 更新已有订单
    /// </summary>
    /// <param name="order">要更新的订单</param>
    /// <param name="ct">取消令牌</param>
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
