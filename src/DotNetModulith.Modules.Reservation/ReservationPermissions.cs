namespace DotNetModulith.Modules.Reservation;

public static class ReservationPermissions
{
    public const string ReservationsView = "reservations.view";
    public const string ReservationsManage = "reservations.manage";

    public static readonly IReadOnlyList<string> All = [ReservationsView, ReservationsManage];
}
