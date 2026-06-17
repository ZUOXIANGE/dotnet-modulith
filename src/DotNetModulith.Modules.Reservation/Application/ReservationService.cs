using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Books.Application;
using DotNetModulith.Modules.Members.Application;
using DotNetModulith.Modules.Reservation.Domain;
using DotNetModulith.Modules.Reservation.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Reservation.Application;

internal sealed class ReservationService : IReservationService
{
    private readonly ReservationDbContext _dbContext;
    private readonly IBookService _bookService;
    private readonly IMemberService _memberService;

    public ReservationService(ReservationDbContext dbContext, IBookService bookService, IMemberService memberService)
    {
        _dbContext = dbContext;
        _bookService = bookService;
        _memberService = memberService;
    }

    public async Task<ReservationListItem[]> GetReservationsAsync(string? keyword, string? status, int page, int pageSize, CancellationToken ct)
    {
        var query = _dbContext.Reservations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReservationStatus>(status, true, out var reservationStatus))
            query = query.Where(x => x.Status == reservationStatus);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ReservationListItem(
                x.Id,
                x.BookId,
                string.Empty,
                x.MemberId,
                string.Empty,
                x.ReserveDate,
                x.ExpiryDate,
                x.Status.ToString(),
                x.QueuePosition,
                x.CreatedAt))
            .ToArrayAsync(ct);

        if (items.Length > 0)
        {
            var bookIds = items.Select(x => x.BookId).Distinct();
            var memberIds = items.Select(x => x.MemberId).Distinct();
            var bookTitles = await _bookService.GetBookTitlesByIdsAsync(bookIds, ct);
            var memberNames = await _memberService.GetMemberNamesByIdsAsync(memberIds, ct);
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = items[i] with
                {
                    BookTitle = bookTitles.GetValueOrDefault(items[i].BookId, string.Empty),
                    MemberName = memberNames.GetValueOrDefault(items[i].MemberId, string.Empty)
                };
            }
        }

        return items;
    }

    public async Task<int> GetReservationsCountAsync(string? keyword, string? status, CancellationToken ct)
    {
        var query = _dbContext.Reservations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReservationStatus>(status, true, out var reservationStatus))
            query = query.Where(x => x.Status == reservationStatus);

        return await query.CountAsync(ct);
    }

    public async Task<ReservationDetails?> GetReservationByIdAsync(Guid id, CancellationToken ct)
    {
        var reservation = await _dbContext.Reservations
            .Where(x => x.Id == id)
            .Select(x => new ReservationDetails(
                x.Id,
                x.BookId,
                string.Empty,
                x.MemberId,
                string.Empty,
                x.ReserveDate,
                x.ExpiryDate,
                x.Status.ToString(),
                x.QueuePosition,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        if (reservation is not null)
        {
            var bookTitles = await _bookService.GetBookTitlesByIdsAsync(new[] { reservation.BookId }, ct);
            var memberNames = await _memberService.GetMemberNamesByIdsAsync(new[] { reservation.MemberId }, ct);
            reservation = reservation with
            {
                BookTitle = bookTitles.GetValueOrDefault(reservation.BookId, string.Empty),
                MemberName = memberNames.GetValueOrDefault(reservation.MemberId, string.Empty)
            };
        }

        return reservation;
    }

    public async Task<ReservationListItem[]> GetReservationsByBookAsync(Guid bookId, CancellationToken ct)
    {
        var items = await _dbContext.Reservations
            .Where(x => x.BookId == bookId && x.Status == ReservationStatus.Pending)
            .OrderBy(x => x.QueuePosition)
            .Select(x => new ReservationListItem(
                x.Id,
                x.BookId,
                string.Empty,
                x.MemberId,
                string.Empty,
                x.ReserveDate,
                x.ExpiryDate,
                x.Status.ToString(),
                x.QueuePosition,
                x.CreatedAt))
            .ToArrayAsync(ct);

        if (items.Length > 0)
        {
            var bookTitles = await _bookService.GetBookTitlesByIdsAsync(new[] { bookId }, ct);
            var memberIds = items.Select(x => x.MemberId).Distinct();
            var memberNames = await _memberService.GetMemberNamesByIdsAsync(memberIds, ct);
            var bookTitle = bookTitles.GetValueOrDefault(bookId, string.Empty);
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = items[i] with
                {
                    BookTitle = bookTitle,
                    MemberName = memberNames.GetValueOrDefault(items[i].MemberId, string.Empty)
                };
            }
        }

        return items;
    }

    public async Task<ReservationListItem[]> GetReservationsByMemberAsync(Guid memberId, CancellationToken ct)
    {
        var items = await _dbContext.Reservations
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReservationListItem(
                x.Id,
                x.BookId,
                string.Empty,
                x.MemberId,
                string.Empty,
                x.ReserveDate,
                x.ExpiryDate,
                x.Status.ToString(),
                x.QueuePosition,
                x.CreatedAt))
            .ToArrayAsync(ct);

        if (items.Length > 0)
        {
            var bookIds = items.Select(x => x.BookId).Distinct();
            var bookTitles = await _bookService.GetBookTitlesByIdsAsync(bookIds, ct);
            var memberNames = await _memberService.GetMemberNamesByIdsAsync(new[] { memberId }, ct);
            var memberName = memberNames.GetValueOrDefault(memberId, string.Empty);
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = items[i] with
                {
                    BookTitle = bookTitles.GetValueOrDefault(items[i].BookId, string.Empty),
                    MemberName = memberName
                };
            }
        }

        return items;
    }

    public async Task<ReservationDetails> CreateReservationAsync(CreateReservationInput input, CancellationToken ct)
    {
        var book = await _bookService.GetBookByIdAsync(input.BookId, ct);
        if (book is null)
            throw new BusinessException("book not found", ApiCodes.Common.NotFound);

        var member = await _memberService.GetMemberByIdAsync(input.MemberId, ct);
        if (member is null)
            throw new BusinessException("member not found", ApiCodes.Common.NotFound);

        if (member.Status != "Active")
            throw new BusinessException("member is not active", ApiCodes.Common.ValidationFailed);

        var existingPending = await _dbContext.Reservations
            .AnyAsync(x => x.BookId == input.BookId && x.MemberId == input.MemberId && x.Status == ReservationStatus.Pending, ct);
        if (existingPending)
            throw new BusinessException("member already has a pending reservation for this book", ApiCodes.Common.ValidationFailed);

        var maxQueue = await _dbContext.Reservations
            .Where(x => x.BookId == input.BookId && x.Status == ReservationStatus.Pending)
            .MaxAsync(x => (int?)x.QueuePosition, ct) ?? 0;

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.Date);
        var expiryDate = today.AddDays(7);

        var entity = ReservationEntity.Create(input.BookId, input.MemberId, today, expiryDate, maxQueue + 1, now);

        _dbContext.Reservations.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return new ReservationDetails(
            entity.Id,
            entity.BookId,
            book.Title,
            entity.MemberId,
            member.Name,
            entity.ReserveDate,
            entity.ExpiryDate,
            entity.Status.ToString(),
            entity.QueuePosition,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public async Task CancelReservationAsync(Guid reservationId, CancellationToken ct)
    {
        var entity = await _dbContext.Reservations.FindAsync(new object[] { reservationId }, ct);
        if (entity is null)
            throw new BusinessException("reservation not found", ApiCodes.Common.NotFound);

        entity.Cancel(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<ReservationDetails?> FulfillNextReservationAsync(Guid bookId, CancellationToken ct)
    {
        var next = await _dbContext.Reservations
            .Where(x => x.BookId == bookId && x.Status == ReservationStatus.Pending)
            .OrderBy(x => x.QueuePosition)
            .FirstOrDefaultAsync(ct);

        if (next is null)
            return null;

        next.Fulfill(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(ct);

        var book = await _bookService.GetBookByIdAsync(bookId, ct);
        var member = await _memberService.GetMemberByIdAsync(next.MemberId, ct);

        return new ReservationDetails(
            next.Id,
            next.BookId,
            book?.Title ?? string.Empty,
            next.MemberId,
            member?.Name ?? string.Empty,
            next.ReserveDate,
            next.ExpiryDate,
            next.Status.ToString(),
            next.QueuePosition,
            next.CreatedAt,
            next.UpdatedAt);
    }
}
