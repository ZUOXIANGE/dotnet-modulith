namespace DotNetModulith.Modules.Fines.Application;

public sealed record FineListItem(
    Guid Id,
    Guid MemberId,
    string MemberName,
    Guid? BorrowingRecordId,
    decimal Amount,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt);
