namespace DotNetModulith.Modules.Fines.Api.Contracts.Requests;

public sealed record CreateFineRequest
{
    public required Guid MemberId { get; init; }

    public Guid? BorrowingRecordId { get; init; }

    public required decimal Amount { get; init; }

    public required string Reason { get; init; }
}
