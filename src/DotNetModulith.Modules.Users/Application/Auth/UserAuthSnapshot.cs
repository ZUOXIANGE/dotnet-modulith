namespace DotNetModulith.Modules.Users.Application;

public sealed record UserAuthSnapshot(
    Guid UserId,
    string UserName,
    string DisplayName,
    string Email,
    bool IsActive,
    int TokenVersion,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
