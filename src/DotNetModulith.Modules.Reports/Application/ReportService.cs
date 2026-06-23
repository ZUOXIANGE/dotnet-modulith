using DotNetModulith.Modules.Books.Infrastructure;
using DotNetModulith.Modules.Borrowing.Domain;
using DotNetModulith.Modules.Borrowing.Infrastructure;
using DotNetModulith.Modules.Fines.Domain;
using DotNetModulith.Modules.Fines.Infrastructure;
using DotNetModulith.Modules.Members.Infrastructure;
using DotNetModulith.Modules.Reports.Application;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Reports.Application;

internal sealed class ReportService : IReportService
{
    private readonly BorrowingDbContext _borrowingDbContext;
    private readonly BooksDbContext _booksDbContext;
    private readonly FinesDbContext _finesDbContext;
    private readonly MembersDbContext _membersDbContext;
    private readonly IFusionCache _fusionCache;

    public ReportService(
        BorrowingDbContext borrowingDbContext,
        BooksDbContext booksDbContext,
        FinesDbContext finesDbContext,
        MembersDbContext membersDbContext,
        IFusionCache fusionCache)
    {
        _borrowingDbContext = borrowingDbContext;
        _booksDbContext = booksDbContext;
        _finesDbContext = finesDbContext;
        _membersDbContext = membersDbContext;
        _fusionCache = fusionCache;
    }

    public async Task<BorrowingStatistics> GetBorrowingStatisticsAsync(CancellationToken ct)
    {
        return await _fusionCache.GetOrSetAsync(
            "reports:borrowing_statistics",
            async _ =>
            {
                var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

                var totalBorrowings = await _borrowingDbContext.BorrowingRecords.CountAsync(ct);
                var activeBorrowings = await _borrowingDbContext.BorrowingRecords
                    .CountAsync(x => x.Status == BorrowingStatus.Borrowed || x.Status == BorrowingStatus.Overdue, ct);
                var overdueBorrowings = await _borrowingDbContext.BorrowingRecords
                    .CountAsync(x => x.Status == BorrowingStatus.Overdue, ct);
                var returnedToday = await _borrowingDbContext.BorrowingRecords
                    .CountAsync(x => x.ReturnDate == today, ct);
                var totalFinesAmount = await _finesDbContext.Fines
                    .Where(x => x.Status == FineStatus.Unpaid)
                    .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
                var unpaidFinesCount = await _finesDbContext.Fines
                    .CountAsync(x => x.Status == FineStatus.Unpaid, ct);

                return new BorrowingStatistics(
                    totalBorrowings,
                    activeBorrowings,
                    overdueBorrowings,
                    returnedToday,
                    totalFinesAmount,
                    unpaidFinesCount);
            },
            new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(30)
            },
            token: ct);
    }

    public async Task<PopularBookItem[]> GetPopularBooksAsync(int topN, CancellationToken ct)
    {
        return await _fusionCache.GetOrSetAsync(
            $"reports:popular_books:{topN}",
            async _ =>
            {
                var bookIds = await _borrowingDbContext.BorrowingRecords
                    .GroupBy(x => x.BookId)
                    .Select(g => new { BookId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(topN)
                    .ToArrayAsync(ct);

                var bookDict = await _booksDbContext.Books
                    .Where(x => bookIds.Select(b => b.BookId).Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, ct);

                return bookIds
                    .Select(x =>
                    {
                        bookDict.TryGetValue(x.BookId, out var book);
                        return new PopularBookItem(
                            x.BookId,
                            book?.Title ?? string.Empty,
                            book?.Isbn ?? string.Empty,
                            book?.Author ?? string.Empty,
                            x.Count);
                    })
                    .ToArray();
            },
            new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(10)
            },
            token: ct);
    }

    public async Task<OverdueReportItem[]> GetOverdueReportAsync(int page, int pageSize, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

        var overdueRecords = await _borrowingDbContext.BorrowingRecords
            .Where(x => x.Status == BorrowingStatus.Overdue)
            .OrderByDescending(x => x.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);

        var bookIds = overdueRecords.Select(x => x.BookId).Distinct().ToArray();
        var memberIds = overdueRecords.Select(x => x.MemberId).Distinct().ToArray();

        var bookDict = await _booksDbContext.Books
            .Where(x => bookIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var memberDict = await _membersDbContext.Members
            .Where(x => memberIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        return overdueRecords
            .Select(x =>
            {
                bookDict.TryGetValue(x.BookId, out var book);
                memberDict.TryGetValue(x.MemberId, out var member);
                var daysOverdue = today.DayNumber - x.DueDate.DayNumber;
                return new OverdueReportItem(
                    x.Id,
                    x.BookId,
                    book?.Title ?? string.Empty,
                    x.MemberId,
                    member?.Name ?? string.Empty,
                    x.BorrowDate,
                    x.DueDate,
                    daysOverdue);
            })
            .ToArray();
    }

    public async Task<int> GetOverdueReportCountAsync(CancellationToken ct)
    {
        return await _fusionCache.GetOrSetAsync(
            "reports:overdue_count",
            async _ => await _borrowingDbContext.BorrowingRecords
                .CountAsync(x => x.Status == BorrowingStatus.Overdue, ct),
            new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(5)
            },
            token: ct);
    }

    public async Task<DailyBorrowingItem[]> GetDailyBorrowingTrendAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        return await _fusionCache.GetOrSetAsync(
            $"reports:daily_trend:{from}:{to}",
            async _ =>
            {
                var borrowings = await _borrowingDbContext.BorrowingRecords
                    .Where(x => x.BorrowDate >= from && x.BorrowDate <= to)
                    .GroupBy(x => x.BorrowDate)
                    .Select(g => new { Date = g.Key, BorrowCount = g.Count() })
                    .ToArrayAsync(ct);

                var returns = await _borrowingDbContext.BorrowingRecords
                    .Where(x => x.ReturnDate != null && x.ReturnDate >= from && x.ReturnDate <= to)
                    .GroupBy(x => x.ReturnDate!.Value)
                    .Select(g => new { Date = g.Key, ReturnCount = g.Count() })
                    .ToArrayAsync(ct);

                var borrowDict = borrowings.ToDictionary(x => x.Date, x => x.BorrowCount);
                var returnDict = returns.ToDictionary(x => x.Date, x => x.ReturnCount);

                var days = to.DayNumber - from.DayNumber + 1;
                return Enumerable.Range(0, days)
                    .Select(i => from.AddDays(i))
                    .Select(d =>
                    {
                        borrowDict.TryGetValue(d, out var bc);
                        returnDict.TryGetValue(d, out var rc);
                        return new DailyBorrowingItem(d, bc, rc);
                    })
                    .ToArray();
            },
            new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(5)
            },
            token: ct);
    }
}
