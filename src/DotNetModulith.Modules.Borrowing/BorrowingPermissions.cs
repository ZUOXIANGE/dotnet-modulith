namespace DotNetModulith.Modules.Borrowing;

public static class BorrowingPermissions
{
    public const string BorrowingsView = "borrowing.view";
    public const string BorrowingsBorrow = "borrowing.borrow";
    public const string BorrowingsReturn = "borrowing.return";
    public const string BorrowingsRenew = "borrowing.renew";
    public const string BorrowingsMarkLost = "borrowing.mark-lost";
    public const string BorrowingRulesView = "borrowing.rules.view";
    public const string BorrowingRulesManage = "borrowing.rules.manage";

    public static readonly IReadOnlyList<string> All =
    [
        BorrowingsView, BorrowingsBorrow, BorrowingsReturn, BorrowingsRenew, BorrowingsMarkLost,
        BorrowingRulesView, BorrowingRulesManage
    ];
}
