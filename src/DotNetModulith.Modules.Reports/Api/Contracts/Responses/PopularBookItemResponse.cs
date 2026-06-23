namespace DotNetModulith.Modules.Reports.Api.Contracts.Responses;

public sealed record PopularBookItemResponse(
    Guid BookId,
    string Title,
    string Isbn,
    string Author,
    int BorrowCount);
