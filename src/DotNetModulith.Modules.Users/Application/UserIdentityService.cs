using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Users.Domain;
using DotNetModulith.Modules.Users.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Users.Application;

public sealed record CurrentUserDetails(
    Guid Id,
    string UserName,
    string DisplayName,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record UserListItem(
    Guid Id,
    string UserName,
    string DisplayName,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);

public sealed record RoleDetails(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions);

public sealed record PermissionDetails(string Code, string Name, string Description);

public sealed record LoginInput(string UserName, string Password, string? RemoteIp, string? UserAgent);

public sealed record CreateUserInput(
    string UserName,
    string DisplayName,
    string Email,
    string Password,
    IReadOnlyCollection<Guid> RoleIds);

public sealed record UpdateUserInput(string DisplayName, string Email);

public sealed record CreateRoleInput(
    string Name,
    string? Description,
    IReadOnlyCollection<string> Permissions);

public sealed record LoginResult(string AccessToken, DateTimeOffset ExpiresAt, CurrentUserDetails User);

public interface IUserIdentityService
{
    Task<LoginResult> LoginAsync(LoginInput input, CancellationToken cancellationToken);

    Task LogoutAsync(Guid userId, string sessionId, CancellationToken cancellationToken);

    Task<CurrentUserDetails> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);

    Task ChangeCurrentPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserListItem>> GetUsersAsync(CancellationToken cancellationToken);

    Task<CurrentUserDetails> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<CurrentUserDetails> CreateUserAsync(CreateUserInput input, CancellationToken cancellationToken);

    Task<CurrentUserDetails> UpdateUserAsync(Guid userId, UpdateUserInput input, CancellationToken cancellationToken);

    Task AssignRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken);

    Task SetUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken);

    Task ForceLogoutAsync(Guid userId, string? reason, CancellationToken cancellationToken);

    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleDetails>> GetRolesAsync(CancellationToken cancellationToken);

    Task<RoleDetails> CreateRoleAsync(CreateRoleInput input, CancellationToken cancellationToken);

    Task UpdateRolePermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionDetails>> GetPermissionsAsync(CancellationToken cancellationToken);
}

internal sealed class UserIdentityService : IUserIdentityService
{
    private readonly UsersDbContext _dbContext;
    private readonly IPasswordHasher<ModuleUser> _passwordHasher;
    private readonly JwtTokenFactory _jwtTokenFactory;

    public UserIdentityService(
        UsersDbContext dbContext,
        IPasswordHasher<ModuleUser> passwordHasher,
        JwtTokenFactory jwtTokenFactory)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenFactory = jwtTokenFactory;
    }

    public async Task<LoginResult> LoginAsync(LoginInput input, CancellationToken cancellationToken)
    {
        var normalizedUserName = NormalizeUserName(input.UserName);
        var user = await _dbContext.Users
            .AsTracking()
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .ThenInclude(x => x.Permissions)
            .Include(x => x.Sessions)
            .SingleOrDefaultAsync(x => x.UserName == normalizedUserName, cancellationToken);

        if (user is null)
        {
            throw new BusinessException(
                "invalid username or password",
                ApiCodes.Auth.InvalidCredentials,
                StatusCodes.Status401Unauthorized);
        }

        if (!user.IsActive)
        {
            throw new BusinessException(
                "user account is disabled",
                ApiCodes.Auth.UserDisabled,
                StatusCodes.Status403Forbidden);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, input.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new BusinessException(
                "invalid username or password",
                ApiCodes.Auth.InvalidCredentials,
                StatusCodes.Status401Unauthorized);
        }

        var now = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid().ToString("N");
        var token = _jwtTokenFactory.CreateAccessToken(user, sessionId);

        user.RecordLogin(now);
        user.Sessions.Add(UserSession.Create(user.Id, sessionId, input.RemoteIp, input.UserAgent, now, token.ExpiresAt));

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new LoginResult(token.AccessToken, token.ExpiresAt, MapCurrentUser(user));
    }

    public async Task LogoutAsync(Guid userId, string sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.UserSessions
            .AsTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.TokenId == sessionId, cancellationToken);

        if (session is null)
        {
            return;
        }

        session.Revoke("logout", DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrentUserDetails> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
        => await GetUserDetailsAsync(userId, cancellationToken);

    public async Task ChangeCurrentPasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsTracking()
            .Include(x => x.Sessions)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new BusinessException(
                "user not found",
                ApiCodes.Common.NotFound,
                StatusCodes.Status404NotFound);

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new BusinessException(
                "current password is invalid",
                ApiCodes.User.InvalidCurrentPassword,
                StatusCodes.Status400BadRequest);
        }

        var now = DateTimeOffset.UtcNow;
        user.SetPassword(_passwordHasher.HashPassword(user, newPassword), now);
        RevokeActiveSessions(user, "password changed", now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserListItem>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .OrderBy(x => x.UserName)
            .Select(x => new UserListItem(
                x.Id,
                x.UserName,
                x.DisplayName,
                x.Email,
                x.IsActive,
                x.CreatedAt,
                x.LastLoginAt,
                x.Roles.Select(role => role.Role.Name).OrderBy(name => name).ToArray()))
            .ToListAsync(cancellationToken);
    }

    public async Task<CurrentUserDetails> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
        => await GetUserDetailsAsync(userId, cancellationToken);

    public async Task<CurrentUserDetails> CreateUserAsync(CreateUserInput input, CancellationToken cancellationToken)
    {
        var normalizedUserName = NormalizeUserName(input.UserName);
        var normalizedEmail = NormalizeEmail(input.Email);

        if (await _dbContext.Users.AnyAsync(x => x.UserName == normalizedUserName, cancellationToken))
        {
            throw new BusinessException(
                "username already exists",
                ApiCodes.User.UserNameAlreadyExists,
                StatusCodes.Status409Conflict);
        }

        if (await _dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new BusinessException(
                "email already exists",
                ApiCodes.User.EmailAlreadyExists,
                StatusCodes.Status409Conflict);
        }

        await EnsureRolesExistAsync(input.RoleIds, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var user = ModuleUser.Create(normalizedUserName, input.DisplayName.Trim(), normalizedEmail, string.Empty, now);
        user.SetPassword(_passwordHasher.HashPassword(user, input.Password), now);
        user.AssignRoles(input.RoleIds, now);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetCurrentUserAsync(user.Id, cancellationToken);
    }

    public async Task<CurrentUserDetails> UpdateUserAsync(
        Guid userId,
        UpdateUserInput input,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(input.Email);
        var user = await _dbContext.Users
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new BusinessException(
                "user not found",
                ApiCodes.Common.NotFound,
                StatusCodes.Status404NotFound);

        if (await _dbContext.Users.AnyAsync(
                x => x.Id != userId && x.Email == normalizedEmail,
                cancellationToken))
        {
            throw new BusinessException(
                "email already exists",
                ApiCodes.User.EmailAlreadyExists,
                StatusCodes.Status409Conflict);
        }

        user.UpdateProfile(input.DisplayName.Trim(), normalizedEmail, DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserDetailsAsync(userId, cancellationToken);
    }

    public async Task AssignRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsTracking()
            .Include(x => x.Roles)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new BusinessException(
                "user not found",
                ApiCodes.Common.NotFound,
                StatusCodes.Status404NotFound);

        await EnsureRolesExistAsync(roleIds, cancellationToken);
        user.AssignRoles(roleIds, DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsTracking()
            .Include(x => x.Sessions)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new BusinessException(
                "user not found",
                ApiCodes.Common.NotFound,
                StatusCodes.Status404NotFound);

        var now = DateTimeOffset.UtcNow;
        user.SetActive(isActive, now);
        if (!isActive)
        {
            user.IncrementTokenVersion(now);
            RevokeActiveSessions(user, "user disabled", now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ForceLogoutAsync(Guid userId, string? reason, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsTracking()
            .Include(x => x.Sessions)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new BusinessException(
                "user not found",
                ApiCodes.Common.NotFound,
                StatusCodes.Status404NotFound);

        var now = DateTimeOffset.UtcNow;
        user.IncrementTokenVersion(now);
        RevokeActiveSessions(user, string.IsNullOrWhiteSpace(reason) ? "force logout" : reason.Trim(), now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsTracking()
            .Include(x => x.Sessions)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new BusinessException(
                "user not found",
                ApiCodes.Common.NotFound,
                StatusCodes.Status404NotFound);

        var now = DateTimeOffset.UtcNow;
        user.SetPassword(_passwordHasher.HashPassword(user, newPassword), now);
        RevokeActiveSessions(user, "password reset", now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleDetails>> GetRolesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .Include(x => x.Permissions)
            .OrderBy(x => x.Name)
            .Select(x => new RoleDetails(
                x.Id,
                x.Name,
                x.Description,
                x.IsSystem,
                x.Permissions.Select(permission => permission.Permission).OrderBy(code => code).ToArray()))
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleDetails> CreateRoleAsync(CreateRoleInput input, CancellationToken cancellationToken)
    {
        var roleName = input.Name.Trim();
        var permissions = NormalizePermissions(input.Permissions);

        if (await _dbContext.Roles.AnyAsync(x => x.Name == roleName, cancellationToken))
        {
            throw new BusinessException(
                "role name already exists",
                ApiCodes.User.RoleNameAlreadyExists,
                StatusCodes.Status409Conflict);
        }

        EnsurePermissionsExist(permissions);

        var now = DateTimeOffset.UtcNow;
        var role = Role.Create(
            roleName,
            string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            false,
            now);

        role.ReplacePermissions(permissions, now);
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RoleDetails(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystem,
            role.Permissions.Select(x => x.Permission).OrderBy(x => x).ToArray());
    }

    public async Task UpdateRolePermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .AsTracking()
            .Include(x => x.Permissions)
            .SingleOrDefaultAsync(x => x.Id == roleId, cancellationToken)
            ?? throw new BusinessException(
                "role not found",
                ApiCodes.Common.NotFound,
                StatusCodes.Status404NotFound);

        var normalizedPermissions = NormalizePermissions(permissions);
        EnsurePermissionsExist(normalizedPermissions);
        role.ReplacePermissions(normalizedPermissions, DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PermissionDetails>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PermissionDetails> permissions = UserPermissions.Definitions
            .Select(x => new PermissionDetails(x.Code, x.Name, x.Description))
            .ToArray();

        return Task.FromResult(permissions);
    }

    private async Task EnsureRolesExistAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return;
        }

        var count = await _dbContext.Roles.CountAsync(x => roleIds.Contains(x.Id), cancellationToken);
        if (count != roleIds.Distinct().Count())
        {
            throw new BusinessException(
                "one or more roles were not found",
                ApiCodes.User.RoleNotFound,
                StatusCodes.Status400BadRequest);
        }
    }

    private static void EnsurePermissionsExist(IEnumerable<string> permissions)
    {
        var invalidPermission = permissions.FirstOrDefault(permission => !UserPermissions.IsDefined(permission));
        if (invalidPermission is not null)
        {
            throw new BusinessException(
                $"permission '{invalidPermission}' is invalid",
                ApiCodes.User.InvalidPermission,
                StatusCodes.Status400BadRequest);
        }
    }

    private static void RevokeActiveSessions(ModuleUser user, string reason, DateTimeOffset now)
    {
        foreach (var session in user.Sessions.Where(x => x.RevokedAt is null))
        {
            session.Revoke(reason, now);
        }
    }

    private static CurrentUserDetails MapCurrentUser(ModuleUser user)
    {
        var roles = user.Roles
            .Select(x => x.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        var permissions = user.Roles
            .SelectMany(x => x.Role.Permissions.Select(permission => permission.Permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        return new CurrentUserDetails(
            user.Id,
            user.UserName,
            user.DisplayName,
            user.Email,
            user.IsActive,
            roles,
            permissions);
    }

    private async Task<CurrentUserDetails> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .ThenInclude(x => x.Permissions)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new BusinessException(
                "user not found",
                ApiCodes.Common.NotFound,
                StatusCodes.Status404NotFound);

        return MapCurrentUser(user);
    }

    private static IReadOnlyCollection<string> NormalizePermissions(IEnumerable<string> permissions)
        => permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string NormalizeUserName(string userName) => userName.Trim().ToLowerInvariant();

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
