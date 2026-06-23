namespace DotNetModulith.Modules.Reservation.Api.Contracts.Responses;

public sealed record ReservationListResponse(
    ReservationListItemResponse[] Items,
    int Total,
    int Page,
    int PageSize);
