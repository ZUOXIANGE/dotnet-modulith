using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Reservation.Application.Subscribers;

public sealed class BookReturnedSubscriber : ICapSubscribe
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<BookReturnedSubscriber> _logger;

    public BookReturnedSubscriber(IReservationService reservationService, ILogger<BookReturnedSubscriber> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    [CapSubscribe(nameof(BookReturnedIntegrationEvent))]
    public async Task HandleBookReturnedAsync(BookReturnedIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "Book returned, attempting to fulfill next reservation: BookId={BookId}, Title={BookTitle}",
            @event.BookId,
            @event.BookTitle);

        var result = await _reservationService.FulfillNextReservationAsync(@event.BookId, ct);

        if (result is not null)
        {
            _logger.LogInformation(
                "Reservation fulfilled: {BookTitle} for {MemberName}, ReservationId={ReservationId}",
                @event.BookTitle,
                result.MemberName,
                result.Id);
        }
        else
        {
            _logger.LogInformation(
                "No pending reservation for book: {BookTitle}",
                @event.BookTitle);
        }
    }
}