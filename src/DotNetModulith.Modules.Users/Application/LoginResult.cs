namespace DotNetModulith.Modules.Users.Application;

public sealed record LoginResult(string AccessToken, DateTimeOffset ExpiresAt, CurrentUserDetails User);
