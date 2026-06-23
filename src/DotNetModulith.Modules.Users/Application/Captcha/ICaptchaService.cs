namespace DotNetModulith.Modules.Users.Application;

public interface ICaptchaService
{
    CaptchaResult Generate();

    bool Validate(string captchaId, string code);
}
