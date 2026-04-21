namespace DotNetModulith.Modules.Users.Domain;

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
