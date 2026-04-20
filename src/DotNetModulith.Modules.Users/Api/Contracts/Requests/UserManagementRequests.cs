using System.ComponentModel.DataAnnotations;
using DotNetModulith.Abstractions.Validation.Attributes;

namespace DotNetModulith.Modules.Users.Api.Contracts.Requests;

/// <summary>
/// 修改本人密码请求
/// </summary>
public sealed record ChangePasswordRequest
{
    [NotWhiteSpace]
    [StringLength(100, MinimumLength = 8)]
    public required string CurrentPassword { get; init; }

    [NotWhiteSpace]
    [StringLength(100, MinimumLength = 8)]
    public required string NewPassword { get; init; }
}

/// <summary>
/// 创建用户请求
/// </summary>
public sealed record CreateUserRequest
{
    [NotWhiteSpace]
    [StringLength(100)]
    public required string UserName { get; init; }

    [NotWhiteSpace]
    [StringLength(100)]
    public required string DisplayName { get; init; }

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public required string Email { get; init; }

    [NotWhiteSpace]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }

    public IReadOnlyCollection<Guid> RoleIds { get; init; } = [];
}

/// <summary>
/// 编辑用户请求
/// </summary>
public sealed record UpdateUserRequest
{
    [NotWhiteSpace]
    [StringLength(100)]
    public required string DisplayName { get; init; }

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public required string Email { get; init; }
}

/// <summary>
/// 分配用户角色请求
/// </summary>
public sealed record AssignUserRolesRequest
{
    public IReadOnlyCollection<Guid> RoleIds { get; init; } = [];
}

/// <summary>
/// 设置用户状态请求
/// </summary>
public sealed record SetUserStatusRequest
{
    public bool IsActive { get; init; }
}

/// <summary>
/// 强制登出请求
/// </summary>
public sealed record ForceLogoutRequest
{
    [StringLength(200)]
    public string? Reason { get; init; }
}

/// <summary>
/// 重置密码请求
/// </summary>
public sealed record ResetPasswordRequest
{
    [NotWhiteSpace]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }
}

/// <summary>
/// 创建角色请求
/// </summary>
public sealed record CreateRoleRequest
{
    [NotWhiteSpace]
    [StringLength(100)]
    public required string Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public IReadOnlyCollection<string> Permissions { get; init; } = [];
}

/// <summary>
/// 更新角色权限请求
/// </summary>
public sealed record UpdateRolePermissionsRequest
{
    public IReadOnlyCollection<string> Permissions { get; init; } = [];
}
