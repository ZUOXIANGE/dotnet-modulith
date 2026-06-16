namespace DotNetModulith.Modules.Reports.Application;

public sealed record BorrowingStatistics(
    int TotalBorrowings,
    int ActiveBorrowings,
    int OverdueBorrowings,
    int ReturnedToday,
    decimal TotalFinesAmount,
    int UnpaidFinesCount);
