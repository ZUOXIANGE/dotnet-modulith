namespace DotNetModulith.Modules.Users.Application;

public sealed record RoleDetails(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions);
