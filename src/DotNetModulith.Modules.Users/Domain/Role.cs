namespace DotNetModulith.Modules.Users.Domain;

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
