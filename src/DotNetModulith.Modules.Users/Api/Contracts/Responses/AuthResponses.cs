using DotNetModulith.Modules.Users.Application;

namespace DotNetModulith.Modules.Users.Api.Contracts.Responses;

/// <summary>
/// 登录响应
/// </summary>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, CurrentUserResponse User);

internal static class AuthResponseMappings
{
    public static LoginResponse ToResponse(this LoginResult result)
        => new(result.AccessToken, result.ExpiresAt, result.User.ToResponse());
}
