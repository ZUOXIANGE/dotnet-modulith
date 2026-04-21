using System.Security.Claims;

namespace DotNetModulith.Modules.Users.Application;

/// <summary>
/// JWT 会话校验器
/// </summary>
public interface IJwtSessionValidator
{
    Task<ClaimsPrincipal?> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
