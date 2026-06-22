namespace DotNetModulith.Modules.Books.Application;

public sealed record BookListItem(
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
