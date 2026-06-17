using DotNetModulith.Abstractions.Authorization;

namespace DotNetModulith.Modules.Users.Domain;

public static class UserPermissions
{
    public const string UsersView = PermissionCodes.UsersView;
    public const string UsersCreate = PermissionCodes.UsersCreate;
    public const string UsersEdit = PermissionCodes.UsersEdit;
    public const string UsersDelete = PermissionCodes.UsersDelete;
    public const string UsersAssignRoles = PermissionCodes.UsersAssignRoles;

    public const string RolesView = PermissionCodes.RolesView;
    public const string RolesCreate = PermissionCodes.RolesCreate;
    public const string RolesEdit = PermissionCodes.RolesEdit;
    public const string RolesDelete = PermissionCodes.RolesDelete;

    public const string BooksView = PermissionCodes.BooksView;
    public const string BooksCreate = PermissionCodes.BooksCreate;
    public const string BooksEdit = PermissionCodes.BooksEdit;
    public const string BooksDelete = PermissionCodes.BooksDelete;
    public const string BooksImport = PermissionCodes.BooksImport;
    public const string BooksBarcode = PermissionCodes.BooksBarcode;

    public const string CategoriesView = PermissionCodes.CategoriesView;
    public const string CategoriesCreate = PermissionCodes.CategoriesCreate;
    public const string CategoriesEdit = PermissionCodes.CategoriesEdit;
    public const string CategoriesDelete = PermissionCodes.CategoriesDelete;

    public const string MembersView = PermissionCodes.MembersView;
    public const string MembersCreate = PermissionCodes.MembersCreate;
    public const string MembersEdit = PermissionCodes.MembersEdit;
    public const string MembersDelete = PermissionCodes.MembersDelete;
    public const string MemberGroupsView = PermissionCodes.MemberGroupsView;
    public const string MemberGroupsCreate = PermissionCodes.MemberGroupsCreate;
    public const string MemberGroupsEdit = PermissionCodes.MemberGroupsEdit;
    public const string MemberGroupsDelete = PermissionCodes.MemberGroupsDelete;

    public const string BorrowingView = PermissionCodes.BorrowingView;
    public const string BorrowingBorrow = PermissionCodes.BorrowingBorrow;
    public const string BorrowingReturn = PermissionCodes.BorrowingReturn;
    public const string BorrowingRenew = PermissionCodes.BorrowingRenew;
    public const string BorrowingMarkLost = PermissionCodes.BorrowingMarkLost;
    public const string BorrowingRulesView = PermissionCodes.BorrowingRulesView;
    public const string BorrowingRulesManage = PermissionCodes.BorrowingRulesManage;

    public const string ReservationView = PermissionCodes.ReservationView;
    public const string ReservationCreate = PermissionCodes.ReservationCreate;
    public const string ReservationCancel = PermissionCodes.ReservationCancel;
    public const string ReservationFulfill = PermissionCodes.ReservationFulfill;

    public const string FinesView = PermissionCodes.FinesView;
    public const string FinesCreate = PermissionCodes.FinesCreate;
    public const string FinesPay = PermissionCodes.FinesPay;
    public const string FinesWaive = PermissionCodes.FinesWaive;
    public const string FinesRulesView = PermissionCodes.FinesRulesView;
    public const string FinesRulesManage = PermissionCodes.FinesRulesManage;

    public const string ReportsView = PermissionCodes.ReportsView;
    public const string ReportsExport = PermissionCodes.ReportsExport;

    public const string StorageView = PermissionCodes.StorageView;
    public const string StorageUpload = PermissionCodes.StorageUpload;
    public const string StorageDelete = PermissionCodes.StorageDelete;

    public const string AuditView = PermissionCodes.AuditView;

    public const string ModulesView = PermissionCodes.ModulesView;

    public const string NotificationsView = PermissionCodes.NotificationsView;
    public const string NotificationsCreate = PermissionCodes.NotificationsCreate;
    public const string NotificationsSend = PermissionCodes.NotificationsSend;
    public const string NotificationsDelete = PermissionCodes.NotificationsDelete;

    public static readonly IReadOnlyList<PermissionDefinition> Definitions =
    [
        new(UsersView, "查看用户", "用户管理", "允许查看后台用户列表与用户详情"),
        new(UsersCreate, "创建用户", "用户管理", "允许创建后台用户"),
        new(UsersEdit, "编辑用户", "用户管理", "允许编辑用户信息"),
        new(UsersDelete, "删除用户", "用户管理", "允许删除用户"),
        new(UsersAssignRoles, "分配角色", "用户管理", "允许为用户分配角色和强制登出"),

        new(RolesView, "查看角色", "角色管理", "允许查看角色与权限点列表"),
        new(RolesCreate, "创建角色", "角色管理", "允许创建角色"),
        new(RolesEdit, "编辑角色", "角色管理", "允许编辑角色信息与权限"),
        new(RolesDelete, "删除角色", "角色管理", "允许删除角色"),

        new(BooksView, "查看图书", "图书管理", "允许查看图书列表与详情"),
        new(BooksCreate, "创建图书", "图书管理", "允许创建图书"),
        new(BooksEdit, "编辑图书", "图书管理", "允许编辑图书信息"),
        new(BooksDelete, "删除图书", "图书管理", "允许删除图书"),
        new(BooksImport, "批量导入", "图书管理", "允许通过 Excel 批量导入图书"),
        new(BooksBarcode, "条码管理", "图书管理", "允许生成和打印图书条码"),

        new(CategoriesView, "查看分类", "分类管理", "允许查看图书分类"),
        new(CategoriesCreate, "创建分类", "分类管理", "允许创建图书分类"),
        new(CategoriesEdit, "编辑分类", "分类管理", "允许编辑图书分类"),
        new(CategoriesDelete, "删除分类", "分类管理", "允许删除图书分类"),

        new(MembersView, "查看读者", "读者管理", "允许查看读者列表与详情"),
        new(MembersCreate, "创建读者", "读者管理", "允许创建读者"),
        new(MembersEdit, "编辑读者", "读者管理", "允许编辑读者信息"),
        new(MembersDelete, "删除读者", "读者管理", "允许删除读者"),
        new(MemberGroupsView, "查看分组", "读者管理", "允许查看读者分组"),
        new(MemberGroupsCreate, "创建分组", "读者管理", "允许创建读者分组"),
        new(MemberGroupsEdit, "编辑分组", "读者管理", "允许编辑读者分组"),
        new(MemberGroupsDelete, "删除分组", "读者管理", "允许删除读者分组"),

        new(BorrowingView, "查看借阅", "借阅管理", "允许查看借阅记录"),
        new(BorrowingBorrow, "借书", "借阅管理", "允许执行借书操作"),
        new(BorrowingReturn, "还书", "借阅管理", "允许执行还书操作"),
        new(BorrowingRenew, "续借", "借阅管理", "允许执行续借操作"),
        new(BorrowingMarkLost, "标记遗失", "借阅管理", "允许标记借阅遗失"),
        new(BorrowingRulesView, "查看规则", "借阅管理", "允许查看借阅规则"),
        new(BorrowingRulesManage, "管理规则", "借阅管理", "允许配置借阅数量、期限等规则"),

        new(ReservationView, "查看预约", "预约管理", "允许查看预约记录"),
        new(ReservationCreate, "创建预约", "预约管理", "允许创建预约"),
        new(ReservationCancel, "取消预约", "预约管理", "允许取消预约"),
        new(ReservationFulfill, "履行预约", "预约管理", "允许处理预约履行"),

        new(FinesView, "查看罚款", "罚款管理", "允许查看罚款记录"),
        new(FinesCreate, "创建罚款", "罚款管理", "允许创建罚款记录"),
        new(FinesPay, "缴纳罚款", "罚款管理", "允许处理罚款缴纳"),
        new(FinesWaive, "豁免罚款", "罚款管理", "允许处理罚款豁免"),
        new(FinesRulesView, "查看规则", "罚款管理", "允许查看罚款规则"),
        new(FinesRulesManage, "管理规则", "罚款管理", "允许配置罚款规则"),

        new(ReportsView, "查看报表", "报表统计", "允许查看统计报表"),
        new(ReportsExport, "导出报表", "报表统计", "允许导出统计报表"),

        new(StorageView, "查看文件", "文件存储", "允许查看存储文件列表"),
        new(StorageUpload, "上传文件", "文件存储", "允许上传文件"),
        new(StorageDelete, "删除文件", "文件存储", "允许删除文件"),

        new(AuditView, "查看日志", "操作日志", "允许查看系统操作日志"),

        new(ModulesView, "查看模块", "系统模块", "允许查看模块列表、依赖图和边界校验结果"),

        new(NotificationsView, "查看通知", "通知管理", "允许查看通知列表"),
        new(NotificationsCreate, "创建通知", "通知管理", "允许创建通知"),
        new(NotificationsSend, "发送通知", "通知管理", "允许发送通知"),
        new(NotificationsDelete, "删除通知", "通知管理", "允许删除通知")
    ];

    public static readonly IReadOnlyList<string> All = Definitions.Select(x => x.Code).ToArray();

    public static bool IsDefined(string permission)
        => Definitions.Any(x => string.Equals(x.Code, permission, StringComparison.OrdinalIgnoreCase));
}
