namespace DotNetModulith.Modules.Books.Api.Contracts.Responses;

public sealed record BookDetailsResponse(
    Guid Id,
    string Isbn,
    string Title,
    string Author,
    string Publisher,
    DateOnly PublishDate,
    string Description,
    Guid CategoryId,
    string CategoryName,
    int TotalCopies,
    int AvailableCopies,
    string CoverImageUrl,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
