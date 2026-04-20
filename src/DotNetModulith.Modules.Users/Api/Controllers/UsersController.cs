using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Users.Api.Contracts.Requests;
using DotNetModulith.Modules.Users.Api.Contracts.Responses;
using DotNetModulith.Modules.Users.Api.Mappings;
using DotNetModulith.Modules.Users.Application;
using DotNetModulith.Modules.Users.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Users.Api.Controllers;

/// <summary>
/// 用户管理接口
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserIdentityService _identityService;

    public UsersController(IUserIdentityService identityService)
    {
        _identityService = identityService;
    }

    /// <summary>
    /// 查询用户列表
    /// </summary>
    [Authorize(Policy = UserPermissions.UsersView)]
    [HttpGet]
    public async Task<ApiResponse<IReadOnlyList<UserListItemResponse>>> GetUsers(CancellationToken ct)
    {
        var users = await _identityService.GetUsersAsync(ct);
        return ApiResponse.Success<IReadOnlyList<UserListItemResponse>>(users.Select(x => x.ToResponse()).ToArray());
    }

    /// <summary>
    /// 查询指定用户详情
    /// </summary>
    [Authorize(Policy = UserPermissions.UsersView)]
    [HttpGet("{userId:guid}")]
    public async Task<ApiResponse<CurrentUserResponse>> GetUser(Guid userId, CancellationToken ct)
    {
        var user = await _identityService.GetUserByIdAsync(userId, ct);
        return ApiResponse.Success(user.ToResponse());
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [Authorize(Policy = UserPermissions.UsersManage)]
    [HttpPost]
    public async Task<ApiResponse<CurrentUserResponse>> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var user = await _identityService.CreateUserAsync(
            new CreateUserInput(request.UserName, request.DisplayName, request.Email, request.Password, request.RoleIds),
            ct);

        return ApiResponse.Success(user.ToResponse());
    }

    /// <summary>
    /// 编辑用户基础资料
    /// </summary>
    [Authorize(Policy = UserPermissions.UsersManage)]
    [HttpPut("{userId:guid}")]
    public async Task<ApiResponse<CurrentUserResponse>> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _identityService.UpdateUserAsync(
            userId,
            new UpdateUserInput(request.DisplayName, request.Email),
            ct);

        return ApiResponse.Success(user.ToResponse());
    }

    /// <summary>
    /// 分配用户角色
    /// </summary>
    [Authorize(Policy = UserPermissions.UsersManage)]
    [HttpPut("{userId:guid}/roles")]
    public async Task<ApiResponse<object?>> AssignRoles(Guid userId, [FromBody] AssignUserRolesRequest request, CancellationToken ct)
    {
        await _identityService.AssignRolesAsync(userId, request.RoleIds, ct);
        return ApiResponse.Success();
    }

    /// <summary>
    /// 设置用户状态
    /// </summary>
    [Authorize(Policy = UserPermissions.UsersManage)]
    [HttpPut("{userId:guid}/status")]
    public async Task<ApiResponse<object?>> SetUserStatus(Guid userId, [FromBody] SetUserStatusRequest request, CancellationToken ct)
    {
        await _identityService.SetUserStatusAsync(userId, request.IsActive, ct);
        return ApiResponse.Success();
    }

    /// <summary>
    /// 强制用户登出
    /// </summary>
    [Authorize(Policy = UserPermissions.UsersManage)]
    [HttpPost("{userId:guid}/force-logout")]
    public async Task<ApiResponse<object?>> ForceLogout(Guid userId, [FromBody] ForceLogoutRequest request, CancellationToken ct)
    {
        await _identityService.ForceLogoutAsync(userId, request.Reason, ct);
        return ApiResponse.Success();
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    [Authorize(Policy = UserPermissions.UsersManage)]
    [HttpPost("{userId:guid}/reset-password")]
    public async Task<ApiResponse<object?>> ResetPassword(Guid userId, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _identityService.ResetPasswordAsync(userId, request.Password, ct);
        return ApiResponse.Success();
    }
}
