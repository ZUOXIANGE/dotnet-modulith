namespace DotNetModulith.Modules.Orders.Application.Caching;

/// <summary>
/// 订单模块缓存配置选项
/// </summary>
public sealed class OrdersCacheOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Caching:Orders";

    /// <summary>
    /// 缓存持续时间
    /// </summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 内存缓存持续时间
    /// </summary>
    public TimeSpan MemoryCacheDuration { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 分布式缓存持续时间
    /// </summary>
    public TimeSpan DistributedCacheDuration { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 是否启用故障安全机制
    /// </summary>
    public bool EnableFailSafe { get; init; } = true;

    /// <summary>
    /// 故障安全最大持续时间
    /// </summary>
    public TimeSpan FailSafeMaxDuration { get; init; } = TimeSpan.FromMinutes(30);
}
