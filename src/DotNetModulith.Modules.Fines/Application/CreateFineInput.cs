namespace DotNetModulith.Modules.Fines.Application;

public sealed record CreateFineInput(
    Guid MemberId,
    Guid? BorrowingRecordId,
    decimal Amount,
    string Reason);
