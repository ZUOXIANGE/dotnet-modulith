namespace DotNetModulith.Modules.Reports.Application;

public sealed record OverdueReportItem(
    Guid BorrowingId,
    Guid BookId,
    string BookTitle,
    Guid MemberId,
    string MemberName,
    DateOnly BorrowDate,
    DateOnly DueDate,
    int DaysOverdue);
