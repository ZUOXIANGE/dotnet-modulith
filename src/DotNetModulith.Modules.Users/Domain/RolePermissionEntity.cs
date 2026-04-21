namespace DotNetModulith.Modules.Users.Domain;

/// <summary>
/// 角色权限关系
/// </summary>
public sealed class RolePermissionEntity
{
    public Guid RoleId { get; private set; }
    public string Permission { get; private set; } = string.Empty;
    public RoleEntity RoleEntity { get; private set; } = default!;

    private RolePermissionEntity()
    {
    }

    public RolePermissionEntity(Guid roleId, string permission)
    {
        RoleId = roleId;
        Permission = permission;
    }
}
