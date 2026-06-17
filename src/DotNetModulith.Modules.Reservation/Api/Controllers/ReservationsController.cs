using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Reservation.Api.Contracts.Requests;
using DotNetModulith.Modules.Reservation.Api.Contracts.Responses;
using DotNetModulith.Modules.Reservation.Api.Mappings;
using DotNetModulith.Modules.Reservation.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Reservation.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [Authorize(Policy = ReservationPermissions.ReservationsView)]
    [HttpGet]
    public async Task<ApiResponse<ReservationListResponse>> GetReservations(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var items = await _reservationService.GetReservationsAsync(null, status, page, pageSize, ct);
        var total = await _reservationService.GetReservationsCountAsync(null, status, ct);

        return ApiResponse.Success(new ReservationListResponse(
            items.Select(x => x.ToResponse()).ToArray(),
            total,
            page,
            pageSize));
    }

    [Authorize(Policy = ReservationPermissions.ReservationsView)]
    [HttpGet("{reservationId:guid}")]
    public async Task<ApiResponse<ReservationDetailsResponse>> GetReservation(Guid reservationId, CancellationToken ct)
    {
        var reservation = await _reservationService.GetReservationByIdAsync(reservationId, ct);
        if (reservation is null)
            return ApiResponse.Failure<ReservationDetailsResponse>("reservation not found", ApiCodes.Common.NotFound);

        return ApiResponse.Success(reservation.ToResponse());
    }

    [Authorize(Policy = ReservationPermissions.ReservationsView)]
    [HttpGet("by-book/{bookId:guid}")]
    public async Task<ApiResponse<ReservationListItemResponse[]>> GetReservationsByBook(Guid bookId, CancellationToken ct)
    {
        var items = await _reservationService.GetReservationsByBookAsync(bookId, ct);
        return ApiResponse.Success(items.Select(x => x.ToResponse()).ToArray());
    }

    [Authorize(Policy = ReservationPermissions.ReservationsView)]
    [HttpGet("by-member/{memberId:guid}")]
    public async Task<ApiResponse<ReservationListItemResponse[]>> GetReservationsByMember(Guid memberId, CancellationToken ct)
    {
        var items = await _reservationService.GetReservationsByMemberAsync(memberId, ct);
        return ApiResponse.Success(items.Select(x => x.ToResponse()).ToArray());
    }

    [Authorize(Policy = ReservationPermissions.ReservationsCreate)]
    [HttpPost]
    public async Task<ApiResponse<ReservationDetailsResponse>> CreateReservation([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var input = new CreateReservationInput(request.BookId, request.MemberId);
        var result = await _reservationService.CreateReservationAsync(input, ct);
        return ApiResponse.Success(result.ToResponse());
    }

    [Authorize(Policy = ReservationPermissions.ReservationsCancel)]
    [HttpDelete("{reservationId:guid}")]
    public async Task<ApiResponse<object?>> CancelReservation(Guid reservationId, CancellationToken ct)
    {
        await _reservationService.CancelReservationAsync(reservationId, ct);
        return ApiResponse.Success();
    }

    [Authorize(Policy = ReservationPermissions.ReservationsFulfill)]
    [HttpPost("fulfill-next")]
    public async Task<ApiResponse<ReservationDetailsResponse>> FulfillReservation([FromBody] FulfillReservationRequest request, CancellationToken ct)
    {
        var result = await _reservationService.FulfillNextReservationAsync(request.BookId, ct);
        if (result is null)
            return ApiResponse.Failure<ReservationDetailsResponse>("no pending reservation found for this book", ApiCodes.Common.NotFound);

        return ApiResponse.Success(result.ToResponse());
    }
}
