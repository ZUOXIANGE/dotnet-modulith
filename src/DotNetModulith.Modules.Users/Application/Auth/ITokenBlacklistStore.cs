namespace DotNetModulith.Modules.Users.Application;

public interface ITokenBlacklistStore
{
    Task<bool> IsBlacklistedAsync(string tokenId, CancellationToken cancellationToken);

    Task BlacklistAsync(string tokenId, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}
