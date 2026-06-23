using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DotNetModulith.Modules.Users.Application;

internal sealed class CaptchaService : ICaptchaService
{
    private static readonly char[] CodeChars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();
    private static readonly string[] Colors = ["#1a73e8", "#d93025", "#188038", "#f9ab00", "#9334e6"];

    private readonly CaptchaOptions _options;
    private readonly IDistributedCache _cache;

    public CaptchaService(IOptions<CaptchaOptions> options, IDistributedCache cache)
    {
        _options = options.Value;
        _cache = cache;
    }

    public CaptchaResult Generate()
    {
        var code = GenerateCode(_options.CodeLength);
        var captchaId = Guid.NewGuid().ToString("N");
        var svg = GenerateSvg(code);

        var cacheKey = $"captcha:{captchaId}";
        _cache.SetString(cacheKey, code, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.ExpireMinutes)
        });

        return new CaptchaResult(captchaId, svg, code);
    }

    public bool Validate(string captchaId, string code)
    {
        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(code))
            return false;

        var cacheKey = $"captcha:{captchaId}";
        var storedCode = _cache.GetString(cacheKey);
        if (string.IsNullOrWhiteSpace(storedCode))
            return false;

        _cache.Remove(cacheKey);
        return string.Equals(storedCode, code, StringComparison.OrdinalIgnoreCase);
    }

    private static string GenerateCode(int length)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(CodeChars[RandomNumberGenerator.GetInt32(CodeChars.Length)]);
        }
        return sb.ToString();
    }

    private static string GenerateSvg(string code)
    {
        var width = 120;
        var height = 44;
        var fontSize = 22;
        var charWidth = width / (code.Length + 1);

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");
        sb.Append($"<rect width=\"{width}\" height=\"{height}\" fill=\"#f0f2f5\" rx=\"4\"/>");

        for (var i = 0; i < 6; i++)
        {
            var x1 = RandomNumberGenerator.GetInt32(width);
            var y1 = RandomNumberGenerator.GetInt32(height);
            var x2 = RandomNumberGenerator.GetInt32(width);
            var y2 = RandomNumberGenerator.GetInt32(height);
            var color = Colors[RandomNumberGenerator.GetInt32(Colors.Length)];
            sb.Append($"<line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" stroke=\"{color}\" stroke-width=\"0.5\" opacity=\"0.3\"/>");
        }

        for (var i = 0; i < 30; i++)
        {
            var cx = RandomNumberGenerator.GetInt32(width);
            var cy = RandomNumberGenerator.GetInt32(height);
            var r = RandomNumberGenerator.GetInt32(1, 3);
            sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{r}\" fill=\"#999\" opacity=\"0.4\"/>");
        }

        for (var i = 0; i < code.Length; i++)
        {
            var x = charWidth * i + charWidth / 2 + RandomNumberGenerator.GetInt32(-4, 5);
            var y = height / 2 + fontSize / 2 - 2 + RandomNumberGenerator.GetInt32(-4, 5);
            var rotate = RandomNumberGenerator.GetInt32(-15, 16);
            var color = Colors[RandomNumberGenerator.GetInt32(Colors.Length)];
            sb.Append($"<text x=\"{x}\" y=\"{y}\" font-size=\"{fontSize}\" font-family=\"Arial, sans-serif\" font-weight=\"bold\" fill=\"{color}\" transform=\"rotate({rotate} {x} {y})\" text-anchor=\"middle\">{code[i]}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}
