namespace DotNetModulith.Modules.Borrowing;

public static class BorrowingPermissions
{
    public const string BorrowingsView = "borrowings.view";
    public const string BorrowingsManage = "borrowings.manage";

    public static readonly IReadOnlyList<string> All = [BorrowingsView, BorrowingsManage];
}
