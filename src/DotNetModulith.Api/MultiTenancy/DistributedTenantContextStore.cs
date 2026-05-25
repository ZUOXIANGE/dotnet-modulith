using System.Text.Json;
using DotNetModulith.ModulithCore.MultiTenancy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DotNetModulith.Api.MultiTenancy;

/// <summary>
/// 将当前请求的租户上下文写入分布式缓存，便于跨实例观测与测试验证。
/// </summary>
public sealed class DistributedTenantContextStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly IOptions<ModulithMultiTenancyOptions> _options;

    public DistributedTenantContextStore(
        IDistributedCache cache,
        IOptions<ModulithMultiTenancyOptions> options)
    {
        _cache = cache;
        _options = options;
    }

    public Task StoreAsync(ModulithTenantInfo tenant, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(
            new TenantContextSnapshot(
                tenant.Id ?? tenant.Identifier ?? string.Empty,
                tenant.Identifier ?? string.Empty,
                tenant.Name,
                DateTimeOffset.UtcNow),
            JsonOptions);

        return _cache.SetStringAsync(
            BuildCacheKey(tenant.Identifier ?? string.Empty),
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.Value.ContextCacheTtl
            },
            cancellationToken);
    }

    public static string BuildCacheKey(string tenantIdentifier) => $"tenancy:context:{tenantIdentifier}";
}
