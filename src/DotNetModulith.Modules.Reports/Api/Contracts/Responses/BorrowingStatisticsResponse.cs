namespace DotNetModulith.Modules.Reports.Api.Contracts.Responses;

public sealed record BorrowingStatisticsResponse(
    int TotalBorrowings,
    int ActiveBorrowings,
    int OverdueBorrowings,
    int ReturnedToday,
    decimal TotalFinesAmount,
    int UnpaidFinesCount);
