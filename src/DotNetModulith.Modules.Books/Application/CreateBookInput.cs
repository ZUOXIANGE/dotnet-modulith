namespace DotNetModulith.Modules.Books.Application;

public sealed record CreateBookInput(
    Guid OperatorUserId,
    string Isbn,
    string Title,
    string Author,
    string Publisher,
    DateOnly PublishDate,
    string Description,
    Guid CategoryId,
    int TotalCopies,
    Guid? CoverUploadId);
