namespace DotNetModulith.Modules.Orders.Domain;

/// <summary>
/// 订单行项目数据，用于创建订单时传递行项目信息
/// </summary>
/// <param name="ProductId">产品ID</param>
/// <param name="ProductName">产品名称</param>
/// <param name="Quantity">数量</param>
/// <param name="UnitPrice">单价</param>
public sealed record OrderLineData(string ProductId, string ProductName, int Quantity, decimal UnitPrice);
