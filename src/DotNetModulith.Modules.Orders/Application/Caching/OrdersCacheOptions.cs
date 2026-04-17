namespace DotNetModulith.Modules.Orders.Application.Caching;

public sealed class OrdersCacheOptions
{
    public const string SectionName = "Caching:Orders";

    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan MemoryCacheDuration { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan DistributedCacheDuration { get; init; } = TimeSpan.FromMinutes(5);

    public bool EnableFailSafe { get; init; } = true;

    public TimeSpan FailSafeMaxDuration { get; init; } = TimeSpan.FromMinutes(30);
}
