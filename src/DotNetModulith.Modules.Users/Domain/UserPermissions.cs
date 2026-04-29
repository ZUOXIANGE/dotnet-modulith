using DotNetModulith.Abstractions.Authorization;

namespace DotNetModulith.Modules.Users.Domain;

/// <summary>
/// 系统权限目录
/// </summary>
public static class UserPermissions
{
    public const string UsersView = PermissionCodes.UsersView;
    public const string UsersManage = PermissionCodes.UsersManage;
    public const string RolesView = PermissionCodes.RolesView;
    public const string RolesManage = PermissionCodes.RolesManage;
    public const string OrdersView = PermissionCodes.OrdersView;
    public const string OrdersManage = PermissionCodes.OrdersManage;
    public const string InventoryView = PermissionCodes.InventoryView;
    public const string InventoryManage = PermissionCodes.InventoryManage;
    public const string StorageManage = PermissionCodes.StorageManage;
    public const string ModulesView = PermissionCodes.ModulesView;

    public static readonly IReadOnlyList<PermissionDefinition> Definitions =
    [
        new(UsersView, "查看用户", "允许查看用户列表与用户详情"),
        new(UsersManage, "管理用户", "允许创建用户、分配角色、重置密码和强制登出"),
        new(RolesView, "查看角色", "允许查看角色与权限点列表"),
        new(RolesManage, "管理角色", "允许创建角色并维护角色权限"),
        new(OrdersView, "查看订单", "允许查看订单详情"),
        new(OrdersManage, "管理订单", "允许创建订单、确认订单和清理订单缓存"),
        new(InventoryView, "查看库存", "允许查看库存详情"),
        new(InventoryManage, "管理库存", "允许创建库存和补充库存"),
        new(StorageManage, "管理文件存储", "允许通过直传或签名上传写入对象存储"),
        new(ModulesView, "查看模块", "允许查看模块列表、依赖图和边界校验结果")
    ];

    public static readonly IReadOnlyList<string> All = Definitions.Select(x => x.Code).ToArray();

    public static bool IsDefined(string permission)
        => Definitions.Any(x => string.Equals(x.Code, permission, StringComparison.OrdinalIgnoreCase));
}
