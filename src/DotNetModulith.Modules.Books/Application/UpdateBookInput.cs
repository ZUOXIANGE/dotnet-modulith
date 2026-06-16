namespace DotNetModulith.Modules.Books.Application;

public sealed record UpdateBookInput(
    string Isbn,
    string Title,
    string Author,
    string Publisher,
    DateOnly PublishDate,
    string Description,
    Guid CategoryId,
    int TotalCopies,
    string CoverImageUrl);
