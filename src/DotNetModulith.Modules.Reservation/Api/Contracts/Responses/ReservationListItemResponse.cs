namespace DotNetModulith.Modules.Reservation.Api.Contracts.Responses;

public sealed record ReservationListItemResponse(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid MemberId,
    string MemberName,
    DateOnly ReserveDate,
    DateOnly ExpiryDate,
    string Status,
    int QueuePosition,
    DateTimeOffset CreatedAt);
