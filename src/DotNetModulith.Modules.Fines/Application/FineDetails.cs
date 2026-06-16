namespace DotNetModulith.Modules.Fines.Application;

public sealed record FineDetails(
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
