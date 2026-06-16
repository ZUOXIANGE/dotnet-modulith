using DotNetModulith.Modules.Reservation.Api.Contracts.Responses;
using DotNetModulith.Modules.Reservation.Application;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Reservation.Api.Mappings;

[Mapper]
public static partial class ReservationResponseMapper
{
    public static partial ReservationListItemResponse ToResponse(this ReservationListItem source);

    public static partial ReservationDetailsResponse ToResponse(this ReservationDetails source);
}
