using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Reservation.Api.Contracts.Requests;

public sealed record CreateReservationRequest
{
    public required Guid BookId { get; init; }

    public required Guid MemberId { get; init; }
}
