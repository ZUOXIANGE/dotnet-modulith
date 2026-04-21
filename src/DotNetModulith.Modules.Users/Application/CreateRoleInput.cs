namespace DotNetModulith.Modules.Users.Application;

public sealed record CreateRoleInput(
    string Name,
    string? Description,
    IReadOnlyCollection<string> Permissions);
