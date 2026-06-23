namespace DotNetModulith.Modules.Users.Application;

public sealed record AvatarAccessUrlDetails(
    string AvatarAccessUrl,
    DateTimeOffset ExpiresAtUtc);
