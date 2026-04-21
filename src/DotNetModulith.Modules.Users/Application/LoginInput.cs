namespace DotNetModulith.Modules.Users.Application;

public sealed record LoginInput(string UserName, string Password, string? RemoteIp, string? UserAgent);
