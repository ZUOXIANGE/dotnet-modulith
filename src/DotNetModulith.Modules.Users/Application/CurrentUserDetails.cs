namespace DotNetModulith.Modules.Users.Application;

public sealed record CurrentUserDetails(
    Guid Id,
    string UserName,
    string DisplayName,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
