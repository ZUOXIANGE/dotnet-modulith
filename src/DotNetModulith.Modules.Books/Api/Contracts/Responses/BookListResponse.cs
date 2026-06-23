namespace DotNetModulith.Modules.Books.Api.Contracts.Responses;

public sealed record BookListResponse(
    BookListItemResponse[] Items,
    int Total,
    int Page,
    int PageSize);
