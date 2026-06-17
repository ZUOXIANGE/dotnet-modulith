namespace DotNetModulith.Modules.Books.Application;

public interface IBookService
{
    Task<IReadOnlyList<BookListItem>> GetBooksAsync(string? keyword, Guid? categoryId, int page, int pageSize, CancellationToken ct);
    Task<int> GetBooksCountAsync(string? keyword, Guid? categoryId, CancellationToken ct);
    Task<BookDetails?> GetBookByIdAsync(Guid id, CancellationToken ct);
    Task<BookDetails> CreateBookAsync(CreateBookInput input, CancellationToken ct);
    Task<BookDetails> UpdateBookAsync(Guid id, UpdateBookInput input, CancellationToken ct);
    Task DeleteBookAsync(Guid id, CancellationToken ct);
    Task BorrowBookAsync(Guid bookId, CancellationToken ct);
    Task ReturnBookAsync(Guid bookId, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, string>> GetBookTitlesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
}
