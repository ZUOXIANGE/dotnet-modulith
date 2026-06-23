namespace DotNetModulith.Modules.Fines.Api.Contracts.Responses;

public sealed record FineListResponse(
    FineListItemResponse[] Items,
    int Total,
    int Page,
    int PageSize);
