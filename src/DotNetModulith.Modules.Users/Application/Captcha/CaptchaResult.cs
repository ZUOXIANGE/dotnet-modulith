namespace DotNetModulith.Modules.Users.Application;

public sealed record CaptchaResult(string CaptchaId, string SvgContent, string Code);
