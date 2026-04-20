using System.Security.Claims;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Users.Api.Contracts.Requests;
using DotNetModulith.Modules.Users.Api.Contracts.Responses;
using DotNetModulith.Modules.Users.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Users.Api.Controllers;

/// <summary>
/// 用户认证接口
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserIdentityService _identityService;

    public AuthController(IUserIdentityService identityService)
    {
        _identityService = identityService;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ApiResponse<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _identityService.LoginAsync(
            new LoginInput(
                request.UserName,
                request.Password,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()),
            ct);

        return ApiResponse.Success(result.ToResponse());
    }

    /// <summary>
    /// 当前用户退出登录
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<ApiResponse<object?>> Logout(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var sessionId = User.FindFirstValue(TokenClaimTypes.SessionId)
            ?? throw new BusinessException("invalid token", ApiCodes.Auth.InvalidToken, StatusCodes.Status401Unauthorized);

        await _identityService.LogoutAsync(userId, sessionId, ct);
        return ApiResponse.Success();
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ApiResponse<CurrentUserResponse>> GetCurrentUser(CancellationToken ct)
    {
        var user = await _identityService.GetCurrentUserAsync(GetCurrentUserId(), ct);
        return ApiResponse.Success(user.ToResponse());
    }

    /// <summary>
    /// 修改当前登录用户密码
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ApiResponse<object?>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _identityService.ChangeCurrentPasswordAsync(
            GetCurrentUserId(),
            request.CurrentPassword,
            request.NewPassword,
            ct);

        return ApiResponse.Success();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new BusinessException("invalid token", ApiCodes.Auth.InvalidToken, StatusCodes.Status401Unauthorized);
    }
}
