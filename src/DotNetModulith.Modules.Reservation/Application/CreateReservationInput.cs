namespace DotNetModulith.Modules.Reservation.Application;

public sealed record CreateReservationInput(
    Guid BookId,
    Guid MemberId);
