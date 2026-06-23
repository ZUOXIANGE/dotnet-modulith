namespace DotNetModulith.Modules.Fines.Api.Contracts.Responses;

public sealed record FineDetailsResponse(
    Guid Id,
    Guid MemberId,
    string MemberName,
    Guid? BorrowingRecordId,
    decimal Amount,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset UpdatedAt);
