namespace DotNetModulith.Modules.Users.Domain;

/// <summary>
/// 权限定义
/// </summary>
public sealed record PermissionDefinition(string Code, string Name, string Description);

/// <summary>
/// 系统权限目录
/// </summary>
public static class UserPermissions
{
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage";

    public static readonly IReadOnlyList<PermissionDefinition> Definitions =
    [
        new(UsersView, "查看用户", "允许查看用户列表与用户详情"),
        new(UsersManage, "管理用户", "允许创建用户、分配角色、重置密码和强制登出"),
        new(RolesView, "查看角色", "允许查看角色与权限点列表"),
        new(RolesManage, "管理角色", "允许创建角色并维护角色权限")
    ];

    public static readonly IReadOnlyList<string> All = Definitions.Select(x => x.Code).ToArray();

    public static bool IsDefined(string permission)
        => Definitions.Any(x => string.Equals(x.Code, permission, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// 用户聚合
/// </summary>
public sealed class ModuleUser
{
    public Guid Id { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int TokenVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public ICollection<UserRole> Roles { get; } = [];
    public ICollection<UserSession> Sessions { get; } = [];

    private ModuleUser()
    {
    }

    public static ModuleUser Create(string userName, string displayName, string email, string passwordHash, DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            DisplayName = displayName,
            Email = email,
            PasswordHash = passwordHash,
            IsActive = true,
            TokenVersion = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void AssignRoles(IEnumerable<Guid> roleIds, DateTimeOffset now)
    {
        Roles.Clear();
        foreach (var roleId in roleIds.Distinct())
        {
            Roles.Add(new UserRole(Id, roleId));
        }

        UpdatedAt = now;
    }

    public void UpdateProfile(string displayName, string email, DateTimeOffset now)
    {
        DisplayName = displayName;
        Email = email;
        UpdatedAt = now;
    }

    public void SetPassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        IncrementTokenVersion(now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        UpdatedAt = now;
    }

    public void RecordLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        UpdatedAt = now;
    }

    public void IncrementTokenVersion(DateTimeOffset now)
    {
        TokenVersion++;
        UpdatedAt = now;
    }
}

/// <summary>
/// 角色聚合
/// </summary>
public sealed class Role
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public ICollection<RolePermission> Permissions { get; } = [];

    private Role()
    {
    }

    public static Role Create(string name, string? description, bool isSystem, DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsSystem = isSystem,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void ReplacePermissions(IEnumerable<string> permissions, DateTimeOffset now)
    {
        Permissions.Clear();
        foreach (var permission in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Permissions.Add(new RolePermission(Id, permission));
        }

        UpdatedAt = now;
    }
}

/// <summary>
/// 用户角色关系
/// </summary>
public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public ModuleUser User { get; private set; } = default!;
    public Role Role { get; private set; } = default!;

    private UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}

/// <summary>
/// 角色权限关系
/// </summary>
public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public string Permission { get; private set; } = string.Empty;
    public Role Role { get; private set; } = default!;

    private RolePermission()
    {
    }

    public RolePermission(Guid roleId, string permission)
    {
        RoleId = roleId;
        Permission = permission;
    }
}

/// <summary>
/// 用户登录会话
/// </summary>
public sealed class UserSession
{
    public string TokenId { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public string? RemoteIp { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    private UserSession()
    {
    }

    public static UserSession Create(Guid userId, string tokenId, string? remoteIp, string? userAgent, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
        => new()
        {
            TokenId = tokenId,
            UserId = userId,
            RemoteIp = string.IsNullOrWhiteSpace(remoteIp) ? null : remoteIp.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt
        };

    public void Revoke(string reason, DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevokedReason = reason;
    }
}
