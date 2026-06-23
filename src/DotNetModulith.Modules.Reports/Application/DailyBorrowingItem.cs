namespace DotNetModulith.Modules.Reports.Application;

public sealed record DailyBorrowingItem(
    DateOnly Date,
    int BorrowCount,
    int ReturnCount);
