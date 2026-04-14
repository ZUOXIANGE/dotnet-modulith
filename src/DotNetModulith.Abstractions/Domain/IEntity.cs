namespace DotNetModulith.Abstractions.Domain;

/// <summary>
/// 实体接口，定义具有唯一标识的领域对象
/// </summary>
/// <typeparam name="TId">实体标识类型</typeparam>
public interface IEntity<TId>
{
    /// <summary>
    /// 实体唯一标识
    /// </summary>
    TId Id { get; }
}
