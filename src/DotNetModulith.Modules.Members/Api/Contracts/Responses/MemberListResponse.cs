namespace DotNetModulith.Modules.Members.Api.Contracts.Responses;

public sealed record MemberListResponse(
    MemberListItemResponse[] Items,
    int Total,
    int Page,
    int PageSize);
