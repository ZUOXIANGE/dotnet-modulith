using DotNetModulith.Modules.Orders.Api.Contracts.Requests;
using DotNetModulith.Modules.Orders.Application.Commands.CreateOrder;
using DotNetModulith.Modules.Orders.Domain;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Orders.Api.Mappings;

/// <summary>
/// 订单 API 请求映射器，负责将接口层请求转换为应用层命令
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class OrderRequestMapper
{
    /// <summary>
    /// 将创建订单请求映射为创建订单命令
    /// </summary>
    public static partial CreateOrderCommand ToCommand(this CreateOrderRequest request);

    /// <summary>
    /// 将创建订单行项目请求映射为订单行项目数据
    /// </summary>
    private static partial OrderLineData MapToOrderLineData(CreateOrderLineRequest request);
}
