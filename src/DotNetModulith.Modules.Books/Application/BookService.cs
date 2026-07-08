using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Books.Domain;
using DotNetModulith.Modules.Books.Infrastructure;
using DotNetModulith.Modules.Storage.Api;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Books.Application;

internal sealed class BookService : IBookService
{
    private readonly BooksDbContext _dbContext;
    private readonly IStorageUploadSessionService _storageUploadSessionService;
    private readonly IStorageReadService _storageReadService;

    public BookService(
        BooksDbContext dbContext,
        IStorageUploadSessionService storageUploadSessionService,
        IStorageReadService storageReadService)
    {
        _dbContext = dbContext;
        _storageUploadSessionService = storageUploadSessionService;
        _storageReadService = storageReadService;
    }

    public async Task<IReadOnlyList<BookListItem>> GetBooksAsync(string? keyword, Guid? categoryId, int page, int pageSize, CancellationToken ct)
    {
        var items = await QueryBooksAsync(keyword, categoryId, page, pageSize, ct);
        return await PresignCoverUrlsAsync(items, ct);
    }

    private async Task<IReadOnlyList<BookListItem>> QueryBooksAsync(string? keyword, Guid? categoryId, int page, int pageSize, CancellationToken ct)
    {
        var query = _dbContext.Books
            .Include(x => x.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(x => x.Title.Contains(kw) || x.Isbn.Contains(kw) || x.Author.Contains(kw));
        }

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BookListItem(
                x.Id,
                x.Isbn,
                x.Title,
                x.Author,
                x.Publisher,
                x.PublishDate,
                x.Category != null ? x.Category.Name : string.Empty,
                x.TotalCopies,
                x.AvailableCopies,
                x.CoverImageUrl,
                x.Status.ToString(),
                x.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<int> GetBooksCountAsync(string? keyword, Guid? categoryId, CancellationToken ct)
    {
        var query = _dbContext.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(x => x.Title.Contains(kw) || x.Isbn.Contains(kw) || x.Author.Contains(kw));
        }

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        return await query.CountAsync(ct);
    }

    public async Task<BookDetails?> GetBookByIdAsync(Guid id, CancellationToken ct)
    {
        var details = await _dbContext.Books
            .Include(x => x.Category)
            .Where(x => x.Id == id)
            .Select(x => new BookDetails(
                x.Id,
                x.Isbn,
                x.Title,
                x.Author,
                x.Publisher,
                x.PublishDate,
                x.Description,
                x.CategoryId,
                x.Category != null ? x.Category.Name : string.Empty,
                x.TotalCopies,
                x.AvailableCopies,
                x.CoverImageUrl,
                x.Status.ToString(),
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        if (details is null)
            return null;

        var presigned = await _storageReadService.GetPresignedReadUrlAsync(details.CoverImageUrl, ct);
        return details with { CoverImageUrl = presigned };
    }

    private async Task<List<BookListItem>> PresignCoverUrlsAsync(IReadOnlyList<BookListItem> items, CancellationToken ct)
    {
        if (items.Count == 0)
            return items.ToList();

        var result = new List<BookListItem>(items.Count);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.CoverImageUrl))
            {
                result.Add(item);
                continue;
            }

            var presigned = await _storageReadService.GetPresignedReadUrlAsync(item.CoverImageUrl, ct);
            result.Add(item with { CoverImageUrl = presigned });
        }

        return result;
    }

    public async Task<BookDetails> CreateBookAsync(CreateBookInput input, CancellationToken ct)
    {
        var exists = await _dbContext.Books.AnyAsync(x => x.Isbn == input.Isbn, ct);
        if (exists)
            throw new BusinessException("ISBN already exists", ApiCodes.Common.ValidationFailed, 400);

        var categoryExists = await _dbContext.Categories.AnyAsync(x => x.Id == input.CategoryId, ct);
        if (!categoryExists)
            throw new BusinessException("category not found", ApiCodes.Common.NotFound, 404);

        var coverImageUrl = string.Empty;
        if (input.CoverUploadId.HasValue)
        {
            var cover = await _storageUploadSessionService.ConsumeUploadSessionAsync(
                input.OperatorUserId,
                input.CoverUploadId.Value,
                UploadPurposes.BookCover,
                ct);

            coverImageUrl = cover.ObjectUrl;
        }

        var now = DateTimeOffset.UtcNow;
        var entity = BookEntity.Create(
            input.Isbn,
            input.Title,
            input.Author,
            input.Publisher,
            input.PublishDate,
            input.Description,
            input.CategoryId,
            input.TotalCopies,
            coverImageUrl,
            now);

        _dbContext.Books.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return new BookDetails(
            entity.Id,
            entity.Isbn,
            entity.Title,
            entity.Author,
            entity.Publisher,
            entity.PublishDate,
            entity.Description,
            entity.CategoryId,
            string.Empty,
            entity.TotalCopies,
            entity.AvailableCopies,
            entity.CoverImageUrl,
            entity.Status.ToString(),
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public async Task<BookDetails> UpdateBookAsync(Guid id, UpdateBookInput input, CancellationToken ct)
    {
        var entity = await _dbContext.Books
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            throw new BusinessException("book not found", ApiCodes.Common.NotFound, 404);

        var isbnConflict = await _dbContext.Books.AnyAsync(x => x.Isbn == input.Isbn && x.Id != id, ct);
        if (isbnConflict)
            throw new BusinessException("ISBN already exists", ApiCodes.Common.ValidationFailed, 400);

        var categoryExists = await _dbContext.Categories.AnyAsync(x => x.Id == input.CategoryId, ct);
        if (!categoryExists)
            throw new BusinessException("category not found", ApiCodes.Common.NotFound, 404);

        var coverImageUrl = entity.CoverImageUrl;
        if (input.ClearCoverImage)
        {
            coverImageUrl = string.Empty;
        }
        else if (input.CoverUploadId.HasValue)
        {
            var cover = await _storageUploadSessionService.ConsumeUploadSessionAsync(
                input.OperatorUserId,
                input.CoverUploadId.Value,
                UploadPurposes.BookCover,
                ct);

            coverImageUrl = cover.ObjectUrl;
        }

        entity.UpdateInfo(
            input.Isbn,
            input.Title,
            input.Author,
            input.Publisher,
            input.PublishDate,
            input.Description,
            input.CategoryId,
            input.TotalCopies,
            coverImageUrl,
            DateTimeOffset.UtcNow);

        await _dbContext.SaveChangesAsync(ct);

        return new BookDetails(
            entity.Id,
            entity.Isbn,
            entity.Title,
            entity.Author,
            entity.Publisher,
            entity.PublishDate,
            entity.Description,
            entity.CategoryId,
            entity.Category?.Name ?? string.Empty,
            entity.TotalCopies,
            entity.AvailableCopies,
            entity.CoverImageUrl,
            entity.Status.ToString(),
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public async Task DeleteBookAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.Books.AsTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            throw new BusinessException("book not found", ApiCodes.Common.NotFound, 404);

        _dbContext.Books.Remove(entity);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task BorrowBookAsync(Guid bookId, CancellationToken ct)
    {
        var entity = await _dbContext.Books.AsTracking().FirstOrDefaultAsync(x => x.Id == bookId, ct);
        if (entity is null)
            throw new BusinessException("book not found", ApiCodes.Common.NotFound, 404);

        entity.BorrowCopy(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task ReturnBookAsync(Guid bookId, CancellationToken ct)
    {
        var entity = await _dbContext.Books.AsTracking().FirstOrDefaultAsync(x => x.Id == bookId, ct);
        if (entity is null)
            throw new BusinessException("book not found", ApiCodes.Common.NotFound, 404);

        entity.ReturnCopy(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetBookTitlesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var idSet = ids.ToHashSet();
        if (idSet.Count == 0)
            return new Dictionary<Guid, string>();

        return await _dbContext.Books
            .Where(x => idSet.Contains(x.Id))
            .Select(x => new { x.Id, x.Title })
            .ToDictionaryAsync(x => x.Id, x => x.Title, ct);
    }
}
