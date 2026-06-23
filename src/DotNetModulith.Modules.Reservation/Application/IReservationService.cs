namespace DotNetModulith.Modules.Reservation.Application;

public interface IReservationService
{
    Task<ReservationListItem[]> GetReservationsAsync(string? keyword, string? status, int page, int pageSize, CancellationToken ct);
    Task<int> GetReservationsCountAsync(string? keyword, string? status, CancellationToken ct);
    Task<ReservationDetails?> GetReservationByIdAsync(Guid id, CancellationToken ct);
    Task<ReservationListItem[]> GetReservationsByBookAsync(Guid bookId, CancellationToken ct);
    Task<ReservationListItem[]> GetReservationsByMemberAsync(Guid memberId, CancellationToken ct);
    Task<ReservationDetails> CreateReservationAsync(CreateReservationInput input, CancellationToken ct);
    Task CancelReservationAsync(Guid reservationId, CancellationToken ct);
    Task<ReservationDetails?> FulfillNextReservationAsync(Guid bookId, CancellationToken ct);
}
