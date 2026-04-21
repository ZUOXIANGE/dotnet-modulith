namespace DotNetModulith.Modules.Users.Domain;

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
