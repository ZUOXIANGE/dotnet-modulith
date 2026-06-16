using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Members.Api.Contracts.Requests;
using DotNetModulith.Modules.Members.Api.Contracts.Responses;
using DotNetModulith.Modules.Members.Api.Mappings;
using DotNetModulith.Modules.Members.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Members.Api.Controllers;

[ApiController]
[Route("api/members")]
public sealed class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MembersController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    [Authorize(Policy = MembersPermissions.MembersView)]
    [HttpGet]
    public async Task<ApiResponse<MemberListResponse>> GetMembers(
        [FromQuery] string? keyword,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var members = await _memberService.GetMembersAsync(keyword, status, page, pageSize, ct);
        var total = await _memberService.GetMembersCountAsync(keyword, status, ct);

        return ApiResponse.Success(new MemberListResponse(
            members.Select(x => x.ToResponse()).ToArray(),
            total,
            page,
            pageSize));
    }

    [Authorize(Policy = MembersPermissions.MembersView)]
    [HttpGet("{memberId:guid}")]
    public async Task<ApiResponse<MemberDetailsResponse>> GetMember(Guid memberId, CancellationToken ct)
    {
        var member = await _memberService.GetMemberByIdAsync(memberId, ct);
        if (member is null)
            return ApiResponse.Failure<MemberDetailsResponse>("member not found", ApiCodes.Common.NotFound);

        return ApiResponse.Success(member.ToResponse());
    }

    [Authorize(Policy = MembersPermissions.MembersManage)]
    [HttpPost]
    public async Task<ApiResponse<MemberDetailsResponse>> CreateMember([FromBody] CreateMemberRequest request, CancellationToken ct)
    {
        var input = new CreateMemberInput(
            request.Name,
            request.Phone,
            request.Email,
            request.Address,
            request.MembershipType,
            request.JoinDate,
            request.ExpiryDate);

        var member = await _memberService.CreateMemberAsync(input, ct);
        return ApiResponse.Success(member.ToResponse());
    }

    [Authorize(Policy = MembersPermissions.MembersManage)]
    [HttpPut("{memberId:guid}")]
    public async Task<ApiResponse<MemberDetailsResponse>> UpdateMember(Guid memberId, [FromBody] UpdateMemberRequest request, CancellationToken ct)
    {
        var input = new UpdateMemberInput(
            request.Name,
            request.Phone,
            request.Email,
            request.Address,
            request.MembershipType,
            request.ExpiryDate);

        var member = await _memberService.UpdateMemberAsync(memberId, input, ct);
        return ApiResponse.Success(member.ToResponse());
    }

    [Authorize(Policy = MembersPermissions.MembersManage)]
    [HttpDelete("{memberId:guid}")]
    public async Task<ApiResponse<object?>> DeleteMember(Guid memberId, CancellationToken ct)
    {
        await _memberService.DeleteMemberAsync(memberId, ct);
        return ApiResponse.Success();
    }

    [Authorize(Policy = MembersPermissions.MembersManage)]
    [HttpPost("{memberId:guid}/suspend")]
    public async Task<ApiResponse<object?>> SuspendMember(Guid memberId, CancellationToken ct)
    {
        await _memberService.SuspendMemberAsync(memberId, ct);
        return ApiResponse.Success();
    }

    [Authorize(Policy = MembersPermissions.MembersManage)]
    [HttpPost("{memberId:guid}/activate")]
    public async Task<ApiResponse<object?>> ActivateMember(Guid memberId, CancellationToken ct)
    {
        await _memberService.ActivateMemberAsync(memberId, ct);
        return ApiResponse.Success();
    }
}
