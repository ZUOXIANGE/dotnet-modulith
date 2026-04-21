namespace DotNetModulith.Modules.Payments.Domain;

/// <summary>
/// 支付仓储接口，提供支付的持久化操作
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// 根据支付ID获取支付记录
    /// </summary>
    /// <param name="id">支付ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>支付实例，未找到时返回null</returns>
    Task<PaymentEntity?> GetByIdAsync(PaymentId id, CancellationToken ct = default);

    /// <summary>
    /// 根据订单ID获取支付记录
    /// </summary>
    /// <param name="orderId">订单ID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>支付实例，未找到时返回null</returns>
    Task<PaymentEntity?> GetByOrderIdAsync(string orderId, CancellationToken ct = default);

    /// <summary>
    /// 添加新支付记录
    /// </summary>
    /// <param name="payment">要添加的支付</param>
    /// <param name="ct">取消令牌</param>
    Task AddAsync(PaymentEntity payment, CancellationToken ct = default);

    /// <summary>
    /// 更新已有支付记录
    /// </summary>
    /// <param name="payment">要更新的支付</param>
    /// <param name="ct">取消令牌</param>
    Task UpdateAsync(PaymentEntity payment, CancellationToken ct = default);

    /// <summary>
    /// 提交当前工作单元中的支付变更
    /// </summary>
    /// <param name="ct">取消令牌</param>
    Task SaveChangesAsync(CancellationToken ct = default);
}
