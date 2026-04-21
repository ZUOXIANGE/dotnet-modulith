namespace DotNetModulith.Modules.Users.Application;

public sealed record CreateUserInput(
    string UserName,
    string DisplayName,
    string Email,
    string Password,
    IReadOnlyCollection<Guid> RoleIds);
