namespace DotNetModulith.Modules.Users.Application;

/// <summary>
/// 已签发令牌结果
/// </summary>
public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);
