namespace DotNetModulith.Modules.Users.Domain;

/// <summary>
/// 用户登录会话
/// </summary>
public sealed class UserSession
{
    public string TokenId { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public string? RemoteIp { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    private UserSession()
    {
    }

    public static UserSession Create(Guid userId, string tokenId, string? remoteIp, string? userAgent, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
        => new()
        {
            TokenId = tokenId,
            UserId = userId,
            RemoteIp = string.IsNullOrWhiteSpace(remoteIp) ? null : remoteIp.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt
        };

    public void Revoke(string reason, DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevokedReason = reason;
    }
}
