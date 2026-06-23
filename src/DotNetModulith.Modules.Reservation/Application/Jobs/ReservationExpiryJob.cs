using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Books.Application;
using DotNetModulith.Modules.Members.Application;
using DotNetModulith.Modules.Reservation.Domain;
using DotNetModulith.Modules.Reservation.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickerQ.Utilities.Base;

namespace DotNetModulith.Modules.Reservation.Application.Jobs;

public sealed class ReservationExpiryJob
{
    private readonly ReservationDbContext _dbContext;
    private readonly IBookService _bookService;
    private readonly IMemberService _memberService;
    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<ReservationExpiryJob> _logger;
    private readonly ReservationExpiryOptions _options;

    public ReservationExpiryJob(
        ReservationDbContext dbContext,
        IBookService bookService,
        IMemberService memberService,
        ICapPublisher capPublisher,
        IOptions<ReservationExpiryOptions> options,
        ILogger<ReservationExpiryJob> logger)
    {
        _dbContext = dbContext;
        _bookService = bookService;
        _memberService = memberService;
        _capPublisher = capPublisher;
        _logger = logger;
        _options = options.Value;
    }

    [TickerFunction("Reservation.ExpiryScan", cronExpression: "0 */10 * * * *")]
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

        var expiredReservations = await _dbContext.Reservations
            .Where(x => x.Status == ReservationStatus.Pending && x.ExpiryDate < today)
            .OrderBy(x => x.ExpiryDate)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (expiredReservations.Count == 0)
        {
            _logger.LogDebug("No expired reservations found");
            return;
        }

        _logger.LogInformation("Found {Count} expired reservations", expiredReservations.Count);

        var bookIds = expiredReservations.Select(x => x.BookId).Distinct();
        var memberIds = expiredReservations.Select(x => x.MemberId).Distinct();
        var bookTitles = await _bookService.GetBookTitlesByIdsAsync(bookIds, cancellationToken);
        var memberNames = await _memberService.GetMemberNamesByIdsAsync(memberIds, cancellationToken);

        foreach (var reservation in expiredReservations)
        {
            reservation.MarkExpired(today, DateTimeOffset.UtcNow);

            var bookTitle = bookTitles.GetValueOrDefault(reservation.BookId, string.Empty);
            var memberName = memberNames.GetValueOrDefault(reservation.MemberId, string.Empty);

            await _capPublisher.PublishAsync(
                nameof(ReservationExpiredIntegrationEvent),
                new ReservationExpiredIntegrationEvent(
                    reservation.Id,
                    reservation.BookId,
                    bookTitle,
                    reservation.MemberId,
                    memberName,
                    reservation.ExpiryDate),
                cancellationToken: cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reservation expiry scan completed. Processed {Count} records", expiredReservations.Count);
    }
}
