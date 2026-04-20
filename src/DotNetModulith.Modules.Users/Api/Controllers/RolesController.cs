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
/// 角色与权限管理接口
/// </summary>
[ApiController]
[Route("api/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IUserIdentityService _identityService;

    public RolesController(IUserIdentityService identityService)
    {
        _identityService = identityService;
    }

    /// <summary>
    /// 查询角色列表
    /// </summary>
    [Authorize(Policy = UserPermissions.RolesView)]
    [HttpGet]
    public async Task<ApiResponse<IReadOnlyList<RoleResponse>>> GetRoles(CancellationToken ct)
    {
        var roles = await _identityService.GetRolesAsync(ct);
        return ApiResponse.Success<IReadOnlyList<RoleResponse>>(roles.Select(x => x.ToResponse()).ToArray());
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    [Authorize(Policy = UserPermissions.RolesManage)]
    [HttpPost]
    public async Task<ApiResponse<RoleResponse>> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var role = await _identityService.CreateRoleAsync(new CreateRoleInput(request.Name, request.Description, request.Permissions), ct);
        return ApiResponse.Success(role.ToResponse());
    }

    /// <summary>
    /// 更新角色权限
    /// </summary>
    [Authorize(Policy = UserPermissions.RolesManage)]
    [HttpPut("{roleId:guid}/permissions")]
    public async Task<ApiResponse<object?>> UpdatePermissions(Guid roleId, [FromBody] UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        await _identityService.UpdateRolePermissionsAsync(roleId, request.Permissions, ct);
        return ApiResponse.Success();
    }

    /// <summary>
    /// 查询权限目录
    /// </summary>
    [Authorize(Policy = UserPermissions.RolesView)]
    [HttpGet("permissions")]
    public async Task<ApiResponse<IReadOnlyList<PermissionResponse>>> GetPermissions(CancellationToken ct)
    {
        var permissions = await _identityService.GetPermissionsAsync(ct);
        return ApiResponse.Success<IReadOnlyList<PermissionResponse>>(permissions.Select(x => x.ToResponse()).ToArray());
    }
}
