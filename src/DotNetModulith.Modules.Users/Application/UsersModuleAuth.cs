using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Users.Domain;
using DotNetModulith.Modules.Users.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DotNetModulith.Modules.Users.Application;

/// <summary>
/// JWT 配置
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    [Required]
    public string Issuer { get; init; } = "dotnet-modulith";

    [Required]
    public string Audience { get; init; } = "dotnet-modulith-api";

    [Required]
    [MinLength(32)]
    public string SecretKey { get; init; } = "DotNetModulith-Replace-This-Key-2026";

    [Range(5, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 120;
}

/// <summary>
/// 令牌声明类型定义
/// </summary>
public static class TokenClaimTypes
{
    public const string SessionId = "modulith_session_id";
    public const string TokenVersion = "modulith_token_version";
    public const string Permission = "modulith_permission";
}

/// <summary>
/// 权限要求
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

/// <summary>
/// 权限校验处理器
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(TokenClaimTypes.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// 已签发令牌结果
/// </summary>
public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);

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

/// <summary>
/// JWT 会话校验器
/// </summary>
public interface IJwtSessionValidator
{
    Task<ClaimsPrincipal?> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}

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

/// <summary>
/// 用户模块种子初始化器
/// </summary>
public interface IUsersModuleSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}

internal sealed class UsersModuleSeeder : IUsersModuleSeeder
{
    public const string DefaultAdminUserName = "admin";
    public const string DefaultAdminPassword = "Admin@123456";

    private readonly UsersDbContext _dbContext;
    private readonly IPasswordHasher<ModuleUser> _passwordHasher;

    public UsersModuleSeeder(UsersDbContext dbContext, IPasswordHasher<ModuleUser> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var adminRole = await _dbContext.Roles
            .AsTracking()
            .Include(x => x.Permissions)
            .SingleOrDefaultAsync(x => x.Name == "Admin", cancellationToken);

        if (adminRole is null)
        {
            adminRole = Role.Create("Admin", "系统管理员", true, now);
            adminRole.ReplacePermissions(UserPermissions.All, now);
            _dbContext.Roles.Add(adminRole);
        }
        else
        {
            adminRole.ReplacePermissions(UserPermissions.All, now);
        }

        var adminUser = await _dbContext.Users
            .AsTracking()
            .Include(x => x.Roles)
            .SingleOrDefaultAsync(x => x.UserName == DefaultAdminUserName, cancellationToken);

        if (adminUser is null)
        {
            adminUser = ModuleUser.Create(DefaultAdminUserName, "系统管理员", "admin@modulith.local", string.Empty, now);
            adminUser.SetPassword(_passwordHasher.HashPassword(adminUser, DefaultAdminPassword), now);
            adminUser.AssignRoles([adminRole.Id], now);
            _dbContext.Users.Add(adminUser);
        }
        else
        {
            if (!adminUser.IsActive)
            {
                adminUser.SetActive(true, now);
            }

            adminUser.AssignRoles([adminRole.Id], now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal static class JwtBearerEventsFactory
{
    public static JwtBearerEvents Create() => new()
    {
        OnTokenValidated = async context =>
        {
            var validator = context.HttpContext.RequestServices.GetRequiredService<IJwtSessionValidator>();
            var principal = await validator.ValidateAsync(context.Principal!, context.HttpContext.RequestAborted);
            if (principal is null)
            {
                context.Fail("invalid token session");
                return;
            }

            context.Principal = principal;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Failure("unauthorized", ApiCodes.Common.Unauthorized),
                context.HttpContext.RequestAborted);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Failure("forbidden", ApiCodes.Common.Forbidden),
                context.HttpContext.RequestAborted);
        }
    };
}
