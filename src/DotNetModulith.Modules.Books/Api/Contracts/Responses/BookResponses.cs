namespace DotNetModulith.Modules.Books.Api.Contracts.Responses;

public sealed record BookListItemResponse(
    Guid Id,
    string Isbn,
    string Title,
    string Author,
    string Publisher,
    DateOnly PublishDate,
    string CategoryName,
    int TotalCopies,
    int AvailableCopies,
    string CoverImageUrl,
    string Status,
    DateTimeOffset CreatedAt);
