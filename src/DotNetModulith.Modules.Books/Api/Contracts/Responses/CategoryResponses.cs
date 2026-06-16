namespace DotNetModulith.Modules.Books.Api.Contracts.Responses;

public sealed record CategoryDetailsResponse(
    Guid Id,
    string Name,
    string Description,
    Guid? ParentId,
    string? ParentName,
    int SortOrder,
    DateTimeOffset CreatedAt);
