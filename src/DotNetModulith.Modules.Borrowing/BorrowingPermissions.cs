namespace DotNetModulith.Modules.Borrowing;

public static class BorrowingPermissions
{
    public const string BorrowingsView = "borrowing.view";
    public const string BorrowingsManage = "borrowing.operate";

    public static readonly IReadOnlyList<string> All = [BorrowingsView, BorrowingsManage];
}
