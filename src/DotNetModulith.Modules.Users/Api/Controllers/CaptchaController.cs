using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Users.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Users.Api.Controllers;

/// <summary>
/// 验证码接口
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class CaptchaController : ControllerBase
{
    private readonly ICaptchaService _captchaService;

    public CaptchaController(ICaptchaService captchaService)
    {
        _captchaService = captchaService;
    }

    /// <summary>
    /// 获取图形验证码
    /// </summary>
    [HttpGet("captcha")]
    public ApiResponse<CaptchaResponse> GetCaptcha()
    {
        var result = _captchaService.Generate();
        return ApiResponse.Success(new CaptchaResponse(result.CaptchaId, result.SvgContent));
    }
}

/// <summary>
/// 验证码响应
/// </summary>
public sealed record CaptchaResponse(string CaptchaId, string SvgContent);
