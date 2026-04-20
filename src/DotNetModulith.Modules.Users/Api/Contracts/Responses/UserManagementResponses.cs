using DotNetModulith.Modules.Users.Application;

namespace DotNetModulith.Modules.Users.Api.Contracts.Responses;

/// <summary>
/// 当前用户响应
/// </summary>
public sealed record CurrentUserResponse(
    Guid Id,
    string UserName,
    string DisplayName,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

/// <summary>
/// 用户列表响应
/// </summary>
public sealed record UserListItemResponse(
    Guid Id,
    string UserName,
    string DisplayName,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);

/// <summary>
/// 角色响应
/// </summary>
public sealed record RoleResponse(Guid Id, string Name, string? Description, bool IsSystem, IReadOnlyList<string> Permissions);

/// <summary>
/// 权限响应
/// </summary>
public sealed record PermissionResponse(string Code, string Name, string Description);

internal static class UserResponseMappings
{
    public static CurrentUserResponse ToResponse(this CurrentUserDetails details)
        => new(details.Id, details.UserName, details.DisplayName, details.Email, details.IsActive, details.Roles, details.Permissions);

    public static UserListItemResponse ToResponse(this UserListItem item)
        => new(item.Id, item.UserName, item.DisplayName, item.Email, item.IsActive, item.CreatedAt, item.LastLoginAt, item.Roles);

    public static RoleResponse ToResponse(this RoleDetails role)
        => new(role.Id, role.Name, role.Description, role.IsSystem, role.Permissions);

    public static PermissionResponse ToResponse(this PermissionDetails permission)
        => new(permission.Code, permission.Name, permission.Description);
}
