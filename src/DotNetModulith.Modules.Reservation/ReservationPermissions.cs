namespace DotNetModulith.Modules.Reservation;

public static class ReservationPermissions
{
    public const string ReservationsView = "reservation.view";
    public const string ReservationsManage = "reservation.manage";

    public static readonly IReadOnlyList<string> All = [ReservationsView, ReservationsManage];
}
