namespace DotNetModulith.Modules.Reports.Api.Contracts.Responses;

public sealed record OverdueReportItemResponse(
    Guid BorrowingId,
    Guid BookId,
    string BookTitle,
    Guid MemberId,
    string MemberName,
    DateOnly BorrowDate,
    DateOnly DueDate,
    int DaysOverdue);
