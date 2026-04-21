using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace DotNetModulith.Modules.Users.Application;

internal sealed class JwtSessionValidator : IJwtSessionValidator
{
    private readonly IUserAuthCache _userAuthCache;
    private readonly ITokenBlacklistStore _tokenBlacklistStore;

    public JwtSessionValidator(IUserAuthCache userAuthCache, ITokenBlacklistStore tokenBlacklistStore)
    {
        _userAuthCache = userAuthCache;
        _tokenBlacklistStore = tokenBlacklistStore;
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var sessionId = principal.FindFirstValue(TokenClaimTypes.SessionId)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var tokenVersionValue = principal.FindFirstValue(TokenClaimTypes.TokenVersion);

        if (!Guid.TryParse(userIdValue, out var userId)
            || string.IsNullOrWhiteSpace(sessionId)
            || !int.TryParse(tokenVersionValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tokenVersion))
        {
            return null;
        }

        if (await _tokenBlacklistStore.IsBlacklistedAsync(sessionId, cancellationToken))
        {
            return null;
        }

        var user = await _userAuthCache.GetAsync(userId, cancellationToken);
        if (user is null || !user.IsActive || user.TokenVersion != tokenVersion)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, sessionId),
            new(TokenClaimTypes.SessionId, sessionId),
            new(TokenClaimTypes.TokenVersion, tokenVersion.ToString(CultureInfo.InvariantCulture))
        };

        var expClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (!string.IsNullOrWhiteSpace(expClaim))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Exp, expClaim));
        }

        claims.AddRange(user.Roles.Distinct(StringComparer.OrdinalIgnoreCase).Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(user.Permissions.Distinct(StringComparer.OrdinalIgnoreCase).Select(permission => new Claim(TokenClaimTypes.Permission, permission)));

        var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
