using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNetModulith.Modules.Users.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DotNetModulith.Modules.Users.Application;

/// <summary>
/// JWT 令牌工厂
/// </summary>
public sealed class JwtTokenFactory
{
    private readonly JwtOptions _options;

    public JwtTokenFactory(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public IssuedToken CreateAccessToken(ModuleUser user, string sessionId)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, sessionId),
            new(TokenClaimTypes.SessionId, sessionId),
            new(TokenClaimTypes.TokenVersion, user.TokenVersion.ToString(CultureInfo.InvariantCulture))
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(tokenDescriptor), expiresAt);
    }
}
