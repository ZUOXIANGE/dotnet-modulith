namespace DotNetModulith.Modules.Reservation.Application;

public sealed record ReservationDetails(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid MemberId,
    string MemberName,
    DateOnly ReserveDate,
    DateOnly ExpiryDate,
    string Status,
    int QueuePosition,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
