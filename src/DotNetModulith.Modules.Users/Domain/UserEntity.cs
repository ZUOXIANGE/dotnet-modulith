namespace DotNetModulith.Modules.Users.Domain;

/// <summary>
/// 用户聚合
/// </summary>
public sealed class UserEntity
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
    public ICollection<UserRoleEntity> Roles { get; } = [];

    private UserEntity()
    {
    }

    public static UserEntity Create(string userName, string displayName, string email, string passwordHash, DateTimeOffset now)
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
            Roles.Add(new UserRoleEntity(Id, roleId));
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
