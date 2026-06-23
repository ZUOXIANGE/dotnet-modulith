using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Borrowing.Api.Contracts.Requests;
using DotNetModulith.Modules.Borrowing.Api.Contracts.Responses;
using DotNetModulith.Modules.Borrowing.Api.Mappings;
using DotNetModulith.Modules.Borrowing.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Borrowing.Api.Controllers;

[ApiController]
[Route("api/borrowings")]
public sealed class BorrowingsController : ControllerBase
{
    private readonly IBorrowingService _borrowingService;

    public BorrowingsController(IBorrowingService borrowingService)
    {
        _borrowingService = borrowingService;
    }

    [Authorize(Policy = BorrowingPermissions.BorrowingsView)]
    [HttpGet]
    public async Task<ApiResponse<BorrowingListResponse>> GetBorrowings(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var items = await _borrowingService.GetBorrowingsAsync(null, status, page, pageSize, ct);
        var total = await _borrowingService.GetBorrowingsCountAsync(null, status, ct);

        return ApiResponse.Success(new BorrowingListResponse(
            items.Select(x => x.ToResponse()).ToArray(),
            total,
            page,
            pageSize));
    }

    [Authorize(Policy = BorrowingPermissions.BorrowingsView)]
    [HttpGet("{borrowingId:guid}")]
    public async Task<ApiResponse<BorrowingDetailsResponse>> GetBorrowing(Guid borrowingId, CancellationToken ct)
    {
        var borrowing = await _borrowingService.GetBorrowingByIdAsync(borrowingId, ct);
        if (borrowing is null)
            return ApiResponse.Failure<BorrowingDetailsResponse>("borrowing record not found", ApiCodes.Common.NotFound);

        return ApiResponse.Success(borrowing.ToResponse());
    }

    [Authorize(Policy = BorrowingPermissions.BorrowingsBorrow)]
    [HttpPost("borrow")]
    public async Task<ApiResponse<BorrowingDetailsResponse>> BorrowBook([FromBody] CreateBorrowingRequest request, CancellationToken ct)
    {
        var input = new CreateBorrowingInput(request.BookId, request.MemberId, request.BorrowDays);
        var result = await _borrowingService.BorrowBookAsync(input, ct);
        return ApiResponse.Success(result.ToResponse());
    }

    [Authorize(Policy = BorrowingPermissions.BorrowingsReturn)]
    [HttpPost("{borrowingId:guid}/return")]
    public async Task<ApiResponse<BorrowingDetailsResponse>> ReturnBook(Guid borrowingId, [FromBody] ReturnBorrowingRequest request, CancellationToken ct)
    {
        var input = new ReturnBorrowingInput(borrowingId, request.Notes);
        var result = await _borrowingService.ReturnBookAsync(borrowingId, input, ct);
        return ApiResponse.Success(result.ToResponse());
    }

    [Authorize(Policy = BorrowingPermissions.BorrowingsRenew)]
    [HttpPost("{borrowingId:guid}/renew")]
    public async Task<ApiResponse<BorrowingDetailsResponse>> RenewBorrowing(Guid borrowingId, CancellationToken ct)
    {
        var result = await _borrowingService.RenewBorrowingAsync(borrowingId, ct);
        return ApiResponse.Success(result.ToResponse());
    }

    [Authorize(Policy = BorrowingPermissions.BorrowingsMarkLost)]
    [HttpPost("{borrowingId:guid}/lost")]
    public async Task<ApiResponse<BorrowingDetailsResponse>> MarkLost(Guid borrowingId, CancellationToken ct)
    {
        var result = await _borrowingService.MarkLostAsync(borrowingId, ct);
        return ApiResponse.Success(result.ToResponse());
    }
}
