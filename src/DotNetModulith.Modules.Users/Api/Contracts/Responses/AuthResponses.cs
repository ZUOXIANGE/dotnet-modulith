namespace DotNetModulith.Modules.Users.Api.Contracts.Responses;

/// <summary>
/// 登录响应
/// </summary>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, CurrentUserResponse User);
