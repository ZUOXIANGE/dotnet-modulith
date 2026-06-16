using DotNetModulith.Abstractions.Authorization;

namespace DotNetModulith.Modules.Users.Domain;

public static class UserPermissions
{
    public const string UsersView = PermissionCodes.UsersView;
    public const string UsersManage = PermissionCodes.UsersManage;
    public const string RolesView = PermissionCodes.RolesView;
    public const string RolesManage = PermissionCodes.RolesManage;
    public const string BooksView = PermissionCodes.BooksView;
    public const string BooksManage = PermissionCodes.BooksManage;
    public const string BooksImport = PermissionCodes.BooksImport;
    public const string BooksBarcode = PermissionCodes.BooksBarcode;
    public const string CategoriesManage = PermissionCodes.CategoriesManage;
    public const string MembersView = PermissionCodes.MembersView;
    public const string MembersManage = PermissionCodes.MembersManage;
    public const string MemberGroupsManage = PermissionCodes.MemberGroupsManage;
    public const string BorrowingOperate = PermissionCodes.BorrowingOperate;
    public const string BorrowingView = PermissionCodes.BorrowingView;
    public const string BorrowingRules = PermissionCodes.BorrowingRules;
    public const string ReservationView = PermissionCodes.ReservationView;
    public const string ReservationManage = PermissionCodes.ReservationManage;
    public const string FinesView = PermissionCodes.FinesView;
    public const string FinesManage = PermissionCodes.FinesManage;
    public const string FinesRules = PermissionCodes.FinesRules;
    public const string ReportsView = PermissionCodes.ReportsView;
    public const string StorageManage = PermissionCodes.StorageManage;
    public const string AuditView = PermissionCodes.AuditView;
    public const string ModulesView = PermissionCodes.ModulesView;
    public const string NotificationsView = PermissionCodes.NotificationsView;
    public const string NotificationsManage = PermissionCodes.NotificationsManage;

    public static readonly IReadOnlyList<PermissionDefinition> Definitions =
    [
        new(UsersView, "查看用户", "允许查看后台用户列表与用户详情"),
        new(UsersManage, "管理用户", "允许创建后台用户、分配角色、重置密码和强制登出"),
        new(RolesView, "查看角色", "允许查看角色与权限点列表"),
        new(RolesManage, "管理角色", "允许创建角色并维护角色权限"),
        new(BooksView, "查看图书", "允许查看图书列表与详情"),
        new(BooksManage, "管理图书", "允许创建、编辑和删除图书信息"),
        new(BooksImport, "批量导入图书", "允许通过 Excel 批量导入图书"),
        new(BooksBarcode, "管理条码", "允许生成和打印图书条码"),
        new(CategoriesManage, "管理分类", "允许创建、编辑和删除图书分类"),
        new(MembersView, "查看读者", "允许查看读者列表与详情"),
        new(MembersManage, "管理读者", "允许创建、编辑读者信息，管理读者分组"),
        new(MemberGroupsManage, "管理读者分组", "允许创建、编辑和删除读者分组"),
        new(BorrowingOperate, "借还操作", "允许执行借书、还书、续借操作"),
        new(BorrowingView, "查看借阅", "允许查看借阅记录"),
        new(BorrowingRules, "管理借阅规则", "允许配置借阅数量、期限等规则"),
        new(ReservationView, "查看预约", "允许查看预约记录"),
        new(ReservationManage, "管理预约", "允许处理预约、取消预约"),
        new(FinesView, "查看罚款", "允许查看罚款记录"),
        new(FinesManage, "管理罚款", "允许处理罚款缴纳、减免"),
        new(FinesRules, "管理罚款规则", "允许配置罚款规则"),
        new(ReportsView, "查看报表", "允许查看统计报表"),
        new(StorageManage, "管理文件存储", "允许通过直传或签名上传写入对象存储"),
        new(AuditView, "查看操作日志", "允许查看系统操作日志"),
        new(ModulesView, "查看模块", "允许查看模块列表、依赖图和边界校验结果"),
        new(NotificationsView, "查看通知", "允许查看通知列表"),
        new(NotificationsManage, "管理通知", "允许创建和发送通知")
    ];

    public static readonly IReadOnlyList<string> All = Definitions.Select(x => x.Code).ToArray();

    public static bool IsDefined(string permission)
        => Definitions.Any(x => string.Equals(x.Code, permission, StringComparison.OrdinalIgnoreCase));
}
