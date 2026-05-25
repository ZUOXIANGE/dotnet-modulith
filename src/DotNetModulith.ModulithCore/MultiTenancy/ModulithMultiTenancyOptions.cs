using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.ModulithCore.MultiTenancy;

/// <summary>
/// 多租户配置选项。
/// </summary>
public sealed class ModulithMultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";

    /// <summary>
    /// 请求头中的租户标识名称。
    /// </summary>
    [Required]
    public string HeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>
    /// 将解析后的租户上下文写入分布式缓存的保留时长。
    /// </summary>
    public TimeSpan ContextCacheTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 系统启动时同步到 Finbuckle 租户存储的租户列表。
    /// </summary>
    [MinLength(1)]
    public List<ModulithTenantInfo> Tenants { get; set; } = [];
}
