using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DotNetModulith.Modules.Users.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Users.Application;

internal sealed class JwtSessionValidator : IJwtSessionValidator
{
    private readonly UsersDbContext _dbContext;

    public JwtSessionValidator(UsersDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.Id,
                x.UserName,
                x.DisplayName,
                x.Email,
                x.IsActive,
                x.TokenVersion,
                Roles = x.Roles.Select(role => role.Role.Name).ToArray(),
                Permissions = x.Roles.SelectMany(role => role.Role.Permissions.Select(permission => permission.Permission)).ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null || !user.IsActive || user.TokenVersion != tokenVersion)
        {
            return null;
        }

        var session = await _dbContext.UserSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.TokenId == sessionId, cancellationToken);

        if (session is null || session.RevokedAt is not null)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(TokenClaimTypes.SessionId, sessionId),
            new(TokenClaimTypes.TokenVersion, tokenVersion.ToString(CultureInfo.InvariantCulture))
        };

        claims.AddRange(user.Roles.Distinct(StringComparer.OrdinalIgnoreCase).Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(user.Permissions.Distinct(StringComparer.OrdinalIgnoreCase).Select(permission => new Claim(TokenClaimTypes.Permission, permission)));

        var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
