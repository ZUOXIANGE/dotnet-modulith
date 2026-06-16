namespace DotNetModulith.Modules.Users.Application;

public sealed record UpdateRoleInput(
    string Name,
    string? Description,
    IReadOnlyCollection<string> Permissions);