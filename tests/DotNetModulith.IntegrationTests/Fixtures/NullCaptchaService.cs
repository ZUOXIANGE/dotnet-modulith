using DotNetModulith.Modules.Users.Application;

namespace DotNetModulith.IntegrationTests.Fixtures;

internal sealed class NullCaptchaService : ICaptchaService
{
    public CaptchaResult Generate()
        => new("test-captcha-id", string.Empty, "test");

    public bool Validate(string captchaId, string code)
        => !string.IsNullOrWhiteSpace(captchaId) && !string.IsNullOrWhiteSpace(code);
}
