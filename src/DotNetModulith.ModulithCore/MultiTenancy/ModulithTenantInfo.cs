using Finbuckle.MultiTenant.Abstractions;

namespace DotNetModulith.ModulithCore.MultiTenancy;

/// <summary>
/// 应用统一租户信息对象。
/// </summary>
public sealed class ModulithTenantInfo : ITenantInfo
{
    /// <summary>
    /// 租户唯一标识，同时也是共享库 TenantId 的写入值。
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 面向外部的租户标识，默认通过请求头解析。
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// 租户显示名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 可选连接串，当前共享库模式下未使用。
    /// </summary>
    public string? ConnectionString { get; set; }
}
