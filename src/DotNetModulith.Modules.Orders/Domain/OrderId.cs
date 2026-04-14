namespace DotNetModulith.Modules.Orders.Domain;

/// <summary>
/// 订单ID值对象，封装订单的唯一标识
/// </summary>
/// <param name="Value">订单ID的GUID值</param>
public sealed record OrderId(Guid Value)
{
    /// <summary>
    /// 生成新的订单ID
    /// </summary>
    public static OrderId New() => new(Guid.NewGuid());

    /// <summary>
    /// 返回订单ID的字符串表示
    /// </summary>
    public override string ToString() => Value.ToString();
}
