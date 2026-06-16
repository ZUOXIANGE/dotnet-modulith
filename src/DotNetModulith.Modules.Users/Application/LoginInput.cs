namespace DotNetModulith.Modules.Users.Application;

public sealed record LoginInput(string UserName, string Password, string CaptchaId, string CaptchaCode, string? RemoteIp, string? UserAgent);
