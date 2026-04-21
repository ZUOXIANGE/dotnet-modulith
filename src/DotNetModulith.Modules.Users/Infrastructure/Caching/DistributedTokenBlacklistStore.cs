using DotNetModulith.Modules.Users.Application;
using Microsoft.Extensions.Caching.Distributed;

namespace DotNetModulith.Modules.Users.Infrastructure.Caching;

internal sealed class DistributedTokenBlacklistStore : ITokenBlacklistStore
{
    private readonly IDistributedCache _cache;

    public DistributedTokenBlacklistStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> IsBlacklistedAsync(string tokenId, CancellationToken cancellationToken)
    {
        var value = await _cache.GetStringAsync(BuildTokenKey(tokenId), cancellationToken);
        return value is not null;
    }

    public Task BlacklistAsync(string tokenId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        return _cache.SetStringAsync(
            BuildTokenKey(tokenId),
            "1",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }

    private static string BuildTokenKey(string tokenId) => $"auth:blacklist:token:{tokenId}";
}
