using DotNetModulith.Modules.Users.Api.Contracts.Responses;
using DotNetModulith.Modules.Users.Application;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Users.Api.Mappings;

/// <summary>
/// 用户模块 API 响应映射器，负责将应用层结果转换为接口层响应模型
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserResponseMapper
{
    /// <summary>
    /// 将登录结果映射为登录响应
    /// </summary>
    public static partial LoginResponse ToResponse(this LoginResult result);

    /// <summary>
    /// 将当前用户详情映射为当前用户响应
    /// </summary>
    public static partial CurrentUserResponse ToResponse(this CurrentUserDetails details);

    /// <summary>
    /// 将用户列表项映射为用户列表响应
    /// </summary>
    public static partial UserListItemResponse ToResponse(this UserListItem item);

    /// <summary>
    /// 将角色详情映射为角色响应
    /// </summary>
    public static partial RoleResponse ToResponse(this RoleDetails role);

    /// <summary>
    /// 将权限详情映射为权限响应
    /// </summary>
    public static partial PermissionResponse ToResponse(this PermissionDetails permission);
}
