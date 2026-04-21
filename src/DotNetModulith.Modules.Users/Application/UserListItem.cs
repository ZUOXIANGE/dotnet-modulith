namespace DotNetModulith.Modules.Users.Application;

public sealed record UserListItem(
    Guid Id,
    string UserName,
    string DisplayName,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);
