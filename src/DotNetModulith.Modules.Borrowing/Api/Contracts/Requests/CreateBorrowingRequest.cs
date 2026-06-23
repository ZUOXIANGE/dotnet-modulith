using System.ComponentModel.DataAnnotations;

namespace DotNetModulith.Modules.Borrowing.Api.Contracts.Requests;

public sealed record CreateBorrowingRequest
{
    public required Guid BookId { get; init; }

    public required Guid MemberId { get; init; }

    [Range(1, 60)]
    public int BorrowDays { get; init; } = 30;
}
