namespace DotNetModulith.Modules.Users.Domain;

/// <summary>
/// 用户角色关系
/// </summary>
public sealed class UserRoleEntity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public UserEntity User { get; private set; } = default!;
    public RoleEntity RoleEntity { get; private set; } = default!;

    private UserRoleEntity()
    {
    }

    public UserRoleEntity(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
