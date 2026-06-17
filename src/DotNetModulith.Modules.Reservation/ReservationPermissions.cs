namespace DotNetModulith.Modules.Reservation;

public static class ReservationPermissions
{
    public const string ReservationsView = "reservation.view";
    public const string ReservationsCreate = "reservation.create";
    public const string ReservationsCancel = "reservation.cancel";
    public const string ReservationsFulfill = "reservation.fulfill";

    public static readonly IReadOnlyList<string> All =
    [
        ReservationsView, ReservationsCreate, ReservationsCancel, ReservationsFulfill
    ];
}
