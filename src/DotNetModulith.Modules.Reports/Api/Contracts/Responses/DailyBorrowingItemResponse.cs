namespace DotNetModulith.Modules.Reports.Api.Contracts.Responses;

public sealed record DailyBorrowingItemResponse(
    DateOnly Date,
    int BorrowCount,
    int ReturnCount);
