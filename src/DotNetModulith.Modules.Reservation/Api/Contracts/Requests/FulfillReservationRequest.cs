namespace DotNetModulith.Modules.Reservation.Api.Contracts.Requests;

public sealed record FulfillReservationRequest
{
    public required Guid BookId { get; init; }
}
