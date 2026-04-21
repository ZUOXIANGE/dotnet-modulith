namespace DotNetModulith.Modules.Users.Application;

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
