namespace DotNetModulith.Modules.Users.Api.Contracts.Responses;

/// <summary>
/// 当前用户头像签名访问地址响应
/// </summary>
public sealed record AvatarAccessUrlResponse(
    string AvatarAccessUrl,
    DateTimeOffset ExpiresAtUtc);
