using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Fines.Api.Contracts.Requests;
using DotNetModulith.Modules.Fines.Api.Contracts.Responses;
using DotNetModulith.Modules.Fines.Api.Mappings;
using DotNetModulith.Modules.Fines.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Fines.Api.Controllers;

[ApiController]
[Route("api/fines")]
public sealed class FinesController : ControllerBase
{
    private readonly IFineService _fineService;

    public FinesController(IFineService fineService)
    {
        _fineService = fineService;
    }

    [Authorize(Policy = FinePermissions.FinesView)]
    [HttpGet]
    public async Task<ApiResponse<FineListResponse>> GetFines(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var items = await _fineService.GetFinesAsync(null, status, page, pageSize, ct);
        var total = await _fineService.GetFinesCountAsync(null, status, ct);

        return ApiResponse.Success(new FineListResponse(
            items.Select(x => x.ToResponse()).ToArray(),
            total,
            page,
            pageSize));
    }

    [Authorize(Policy = FinePermissions.FinesView)]
    [HttpGet("{fineId:guid}")]
    public async Task<ApiResponse<FineDetailsResponse>> GetFine(Guid fineId, CancellationToken ct)
    {
        var fine = await _fineService.GetFineByIdAsync(fineId, ct);
        if (fine is null)
            return ApiResponse.Failure<FineDetailsResponse>("fine not found", ApiCodes.Common.NotFound);

        return ApiResponse.Success(fine.ToResponse());
    }

    [Authorize(Policy = FinePermissions.FinesView)]
    [HttpGet("by-member/{memberId:guid}")]
    public async Task<ApiResponse<FineListItemResponse[]>> GetFinesByMember(Guid memberId, CancellationToken ct)
    {
        var items = await _fineService.GetFinesByMemberAsync(memberId, ct);
        return ApiResponse.Success(items.Select(x => x.ToResponse()).ToArray());
    }

    [Authorize(Policy = FinePermissions.FinesCreate)]
    [HttpPost]
    public async Task<ApiResponse<FineDetailsResponse>> CreateFine([FromBody] CreateFineRequest request, CancellationToken ct)
    {
        var input = new CreateFineInput(request.MemberId, request.BorrowingRecordId, request.Amount, request.Reason);
        var result = await _fineService.CreateFineAsync(input, ct);
        return ApiResponse.Success(result.ToResponse());
    }

    [Authorize(Policy = FinePermissions.FinesPay)]
    [HttpPost("{fineId:guid}/pay")]
    public async Task<ApiResponse<object?>> PayFine(Guid fineId, CancellationToken ct)
    {
        await _fineService.PayFineAsync(fineId, ct);
        return ApiResponse.Success();
    }

    [Authorize(Policy = FinePermissions.FinesWaive)]
    [HttpPost("{fineId:guid}/waive")]
    public async Task<ApiResponse<object?>> WaiveFine(Guid fineId, CancellationToken ct)
    {
        await _fineService.WaiveFineAsync(fineId, ct);
        return ApiResponse.Success();
    }
}
