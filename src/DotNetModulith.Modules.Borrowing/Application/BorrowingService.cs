using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Books.Application;
using DotNetModulith.Modules.Borrowing.Domain;
using DotNetModulith.Modules.Borrowing.Infrastructure;
using DotNetModulith.Modules.Members.Application;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Borrowing.Application;

internal sealed class BorrowingService : IBorrowingService
{
    private readonly BorrowingDbContext _dbContext;
    private readonly IBookService _bookService;
    private readonly IMemberService _memberService;

    public BorrowingService(BorrowingDbContext dbContext, IBookService bookService, IMemberService memberService)
    {
        _dbContext = dbContext;
        _bookService = bookService;
        _memberService = memberService;
    }

    public async Task<BorrowingListItem[]> GetBorrowingsAsync(string? keyword, string? status, int page, int pageSize, CancellationToken ct)
    {
        var query = _dbContext.BorrowingRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BorrowingStatus>(status, true, out var borrowingStatus))
            query = query.Where(x => x.Status == borrowingStatus);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BorrowingListItem(
                x.Id,
                x.BookId,
                string.Empty,
                x.MemberId,
                string.Empty,
                x.BorrowDate,
                x.DueDate,
                x.ReturnDate,
                x.Status.ToString(),
                x.RenewalCount,
                x.CreatedAt))
            .ToArrayAsync(ct);
    }

    public async Task<int> GetBorrowingsCountAsync(string? keyword, string? status, CancellationToken ct)
    {
        var query = _dbContext.BorrowingRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BorrowingStatus>(status, true, out var borrowingStatus))
            query = query.Where(x => x.Status == borrowingStatus);

        return await query.CountAsync(ct);
    }

    public async Task<BorrowingDetails?> GetBorrowingByIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.BorrowingRecords
            .Where(x => x.Id == id)
            .Select(x => new BorrowingDetails(
                x.Id,
                x.BookId,
                string.Empty,
                x.MemberId,
                string.Empty,
                x.BorrowDate,
                x.DueDate,
                x.ReturnDate,
                x.Status.ToString(),
                x.RenewalCount,
                x.Notes,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BorrowingDetails> BorrowBookAsync(CreateBorrowingInput input, CancellationToken ct)
    {
        var book = await _bookService.GetBookByIdAsync(input.BookId, ct);
        if (book is null)
            throw new BusinessException("book not found", ApiCodes.Common.NotFound);

        var member = await _memberService.GetMemberByIdAsync(input.MemberId, ct);
        if (member is null)
            throw new BusinessException("member not found", ApiCodes.Common.NotFound);

        if (member.Status != "Active")
            throw new BusinessException("member is not active", ApiCodes.Common.ValidationFailed);

        if (member.CurrentBorrowCount >= member.MaxBorrowCount)
            throw new BusinessException("member has reached maximum borrow limit", ApiCodes.Common.ValidationFailed);

        var activeBorrowings = await _dbContext.BorrowingRecords
            .CountAsync(x => x.MemberId == input.MemberId && x.BookId == input.BookId
                && (x.Status == BorrowingStatus.Borrowed || x.Status == BorrowingStatus.Overdue), ct);
        if (activeBorrowings > 0)
            throw new BusinessException("member has already borrowed this book", ApiCodes.Common.ValidationFailed);

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.Date);
        var dueDate = today.AddDays(input.BorrowDays);

        var entity = BorrowingRecordEntity.Create(input.BookId, input.MemberId, today, dueDate, now);

        await _bookService.BorrowBookAsync(input.BookId, ct);
        await _memberService.IncrementBorrowCountAsync(input.MemberId, ct);

        _dbContext.BorrowingRecords.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return new BorrowingDetails(
            entity.Id,
            entity.BookId,
            book.Title,
            entity.MemberId,
            member.Name,
            entity.BorrowDate,
            entity.DueDate,
            entity.ReturnDate,
            entity.Status.ToString(),
            entity.RenewalCount,
            entity.Notes,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public async Task<BorrowingDetails> ReturnBookAsync(Guid borrowingId, ReturnBorrowingInput input, CancellationToken ct)
    {
        var entity = await _dbContext.BorrowingRecords.FindAsync(new object[] { borrowingId }, ct);
        if (entity is null)
            throw new BusinessException("borrowing record not found", ApiCodes.Common.NotFound);

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.Date);

        entity.Return(today, now);

        if (!string.IsNullOrWhiteSpace(input.Notes))
            entity.Notes = input.Notes;

        await _bookService.ReturnBookAsync(entity.BookId, ct);
        await _memberService.DecrementBorrowCountAsync(entity.MemberId, ct);
        await _dbContext.SaveChangesAsync(ct);

        var book = await _bookService.GetBookByIdAsync(entity.BookId, ct);
        var member = await _memberService.GetMemberByIdAsync(entity.MemberId, ct);

        return new BorrowingDetails(
            entity.Id,
            entity.BookId,
            book?.Title ?? string.Empty,
            entity.MemberId,
            member?.Name ?? string.Empty,
            entity.BorrowDate,
            entity.DueDate,
            entity.ReturnDate,
            entity.Status.ToString(),
            entity.RenewalCount,
            entity.Notes,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public async Task<BorrowingDetails> RenewBorrowingAsync(Guid borrowingId, CancellationToken ct)
    {
        var entity = await _dbContext.BorrowingRecords.FindAsync(new object[] { borrowingId }, ct);
        if (entity is null)
            throw new BusinessException("borrowing record not found", ApiCodes.Common.NotFound);

        var now = DateTimeOffset.UtcNow;
        var newDueDate = entity.DueDate.AddDays(30);

        entity.Renew(newDueDate, now);
        await _dbContext.SaveChangesAsync(ct);

        var book = await _bookService.GetBookByIdAsync(entity.BookId, ct);
        var member = await _memberService.GetMemberByIdAsync(entity.MemberId, ct);

        return new BorrowingDetails(
            entity.Id,
            entity.BookId,
            book?.Title ?? string.Empty,
            entity.MemberId,
            member?.Name ?? string.Empty,
            entity.BorrowDate,
            entity.DueDate,
            entity.ReturnDate,
            entity.Status.ToString(),
            entity.RenewalCount,
            entity.Notes,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public async Task<BorrowingDetails> MarkLostAsync(Guid borrowingId, CancellationToken ct)
    {
        var entity = await _dbContext.BorrowingRecords.FindAsync(new object[] { borrowingId }, ct);
        if (entity is null)
            throw new BusinessException("borrowing record not found", ApiCodes.Common.NotFound);

        entity.MarkLost(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);

        var book = await _bookService.GetBookByIdAsync(entity.BookId, ct);
        var member = await _memberService.GetMemberByIdAsync(entity.MemberId, ct);

        return new BorrowingDetails(
            entity.Id,
            entity.BookId,
            book?.Title ?? string.Empty,
            entity.MemberId,
            member?.Name ?? string.Empty,
            entity.BorrowDate,
            entity.DueDate,
            entity.ReturnDate,
            entity.Status.ToString(),
            entity.RenewalCount,
            entity.Notes,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
