using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Reservation.Application.Jobs;

public sealed class ReservationExpiryOptions
{
    public const string SectionName = "ReservationExpiry";

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;
}
