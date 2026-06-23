namespace DotNetModulith.Modules.Borrowing.Api.Contracts.Responses;

public sealed record BorrowingListResponse(
    BorrowingListItemResponse[] Items,
    int Total,
    int Page,
    int PageSize);
