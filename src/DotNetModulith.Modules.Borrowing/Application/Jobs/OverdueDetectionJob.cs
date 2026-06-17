using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Books.Application;
using DotNetModulith.Modules.Borrowing.Domain;
using DotNetModulith.Modules.Borrowing.Infrastructure;
using DotNetModulith.Modules.Members.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickerQ.Utilities.Base;

namespace DotNetModulith.Modules.Borrowing.Application.Jobs;

public sealed class OverdueDetectionJob
{
    private readonly BorrowingDbContext _dbContext;
    private readonly IBookService _bookService;
    private readonly IMemberService _memberService;
    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<OverdueDetectionJob> _logger;
    private readonly OverdueDetectionOptions _options;

    public OverdueDetectionJob(
        BorrowingDbContext dbContext,
        IBookService bookService,
        IMemberService memberService,
        ICapPublisher capPublisher,
        IOptions<OverdueDetectionOptions> options,
        ILogger<OverdueDetectionJob> logger)
    {
        _dbContext = dbContext;
        _bookService = bookService;
        _memberService = memberService;
        _capPublisher = capPublisher;
        _logger = logger;
        _options = options.Value;
    }

    [TickerFunction("Borrowing.OverdueDetection", cronExpression: "0 */5 * * * *")]
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

        var overdueRecords = await _dbContext.BorrowingRecords
            .Where(x => x.Status == BorrowingStatus.Borrowed && x.DueDate < today)
            .OrderBy(x => x.DueDate)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (overdueRecords.Count == 0)
        {
            _logger.LogDebug("No overdue borrowing records found");
            return;
        }

        _logger.LogInformation("Found {Count} overdue borrowing records", overdueRecords.Count);

        var bookIds = overdueRecords.Select(x => x.BookId).Distinct();
        var memberIds = overdueRecords.Select(x => x.MemberId).Distinct();
        var bookTitles = await _bookService.GetBookTitlesByIdsAsync(bookIds, cancellationToken);
        var memberNames = await _memberService.GetMemberNamesByIdsAsync(memberIds, cancellationToken);

        foreach (var record in overdueRecords)
        {
            record.MarkOverdue(today, DateTimeOffset.UtcNow);

            var bookTitle = bookTitles.GetValueOrDefault(record.BookId, string.Empty);
            var memberName = memberNames.GetValueOrDefault(record.MemberId, string.Empty);
            var overdueDays = today.DayNumber - record.DueDate.DayNumber;

            await _capPublisher.PublishAsync(
                nameof(BookOverdueIntegrationEvent),
                new BookOverdueIntegrationEvent(
                    record.Id,
                    record.BookId,
                    bookTitle,
                    record.MemberId,
                    memberName,
                    record.DueDate,
                    overdueDays),
                cancellationToken: cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Overdue detection completed. Processed {Count} records", overdueRecords.Count);
    }
}
