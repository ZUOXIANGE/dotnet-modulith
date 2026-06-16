using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Users.Application;

public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";

    [Range(4, 8)]
    public int CodeLength { get; set; } = 4;

    [Range(1, 30)]
    public int ExpireMinutes { get; set; } = 5;

    public int Width { get; set; } = 120;

    public int Height { get; set; } = 44;
}
