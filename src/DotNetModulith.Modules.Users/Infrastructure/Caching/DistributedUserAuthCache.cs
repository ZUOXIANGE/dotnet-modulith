using System.Text.Json;
using DotNetModulith.Modules.Users.Application;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DotNetModulith.Modules.Users.Infrastructure.Caching;

internal sealed class DistributedUserAuthCache : IUserAuthCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly TimeSpan _snapshotTtl;

    public DistributedUserAuthCache(IDistributedCache cache, IOptions<AuthCacheOptions> options)
    {
        _cache = cache;
        _snapshotTtl = TimeSpan.FromMinutes(options.Value.UserSnapshotTtlMinutes);
    }

    public async Task<UserAuthSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var payload = await _cache.GetStringAsync(BuildUserKey(userId), cancellationToken);
        return payload is null
            ? null
            : JsonSerializer.Deserialize<UserAuthSnapshot>(payload, JsonOptions);
    }

    public Task SetAsync(UserAuthSnapshot snapshot, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(snapshot, JsonOptions);
        return _cache.SetStringAsync(
            BuildUserKey(snapshot.UserId),
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _snapshotTtl
            },
            cancellationToken);
    }

    public Task RemoveAsync(Guid userId, CancellationToken cancellationToken)
        => _cache.RemoveAsync(BuildUserKey(userId), cancellationToken);

    private static string BuildUserKey(Guid userId) => $"auth:user:{userId:N}";
}
