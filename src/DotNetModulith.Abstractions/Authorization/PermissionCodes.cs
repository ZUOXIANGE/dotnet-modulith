namespace DotNetModulith.Abstractions.Authorization;

public static class PermissionCodes
{
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";
    public const string UsersAssignRoles = "users.assign-roles";

    public const string RolesView = "roles.view";
    public const string RolesCreate = "roles.create";
    public const string RolesEdit = "roles.edit";
    public const string RolesDelete = "roles.delete";

    public const string BooksView = "books.view";
    public const string BooksCreate = "books.create";
    public const string BooksEdit = "books.edit";
    public const string BooksDelete = "books.delete";
    public const string BooksImport = "books.import";
    public const string BooksBarcode = "books.barcode";

    public const string CategoriesView = "categories.view";
    public const string CategoriesCreate = "categories.create";
    public const string CategoriesEdit = "categories.edit";
    public const string CategoriesDelete = "categories.delete";

    public const string MembersView = "members.view";
    public const string MembersCreate = "members.create";
    public const string MembersEdit = "members.edit";
    public const string MembersDelete = "members.delete";
    public const string MemberGroupsView = "members.groups.view";
    public const string MemberGroupsCreate = "members.groups.create";
    public const string MemberGroupsEdit = "members.groups.edit";
    public const string MemberGroupsDelete = "members.groups.delete";

    public const string BorrowingView = "borrowing.view";
    public const string BorrowingBorrow = "borrowing.borrow";
    public const string BorrowingReturn = "borrowing.return";
    public const string BorrowingRenew = "borrowing.renew";
    public const string BorrowingMarkLost = "borrowing.mark-lost";
    public const string BorrowingRulesView = "borrowing.rules.view";
    public const string BorrowingRulesManage = "borrowing.rules.manage";

    public const string ReservationView = "reservation.view";
    public const string ReservationCreate = "reservation.create";
    public const string ReservationCancel = "reservation.cancel";
    public const string ReservationFulfill = "reservation.fulfill";

    public const string FinesView = "fines.view";
    public const string FinesCreate = "fines.create";
    public const string FinesPay = "fines.pay";
    public const string FinesWaive = "fines.waive";
    public const string FinesRulesView = "fines.rules.view";
    public const string FinesRulesManage = "fines.rules.manage";

    public const string ReportsView = "reports.view";
    public const string ReportsExport = "reports.export";

    public const string StorageView = "storage.view";
    public const string StorageUpload = "storage.upload";
    public const string StorageDelete = "storage.delete";

    public const string AuditView = "audit.view";

    public const string ModulesView = "modules.view";

    public const string NotificationsView = "notifications.view";
    public const string NotificationsCreate = "notifications.create";
    public const string NotificationsSend = "notifications.send";
    public const string NotificationsDelete = "notifications.delete";
}
