namespace DotNetModulith.Modules.Reports.Application;

public sealed record PopularBookItem(
    Guid BookId,
    string Title,
    string Isbn,
    string Author,
    int BorrowCount);
