using System.Text.Json.Serialization;

namespace DotNetModulith.Abstractions.Events;

public sealed record BookReturnedIntegrationEvent : IntegrationEvent
{
    public Guid BorrowingRecordId { get; init; }
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public Guid MemberId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public DateOnly ReturnDate { get; init; }

    [JsonConstructor]
    public BookReturnedIntegrationEvent(
        Guid borrowingRecordId,
        Guid bookId,
        string bookTitle,
        Guid memberId,
        string memberName,
        DateOnly returnDate)
    {
        BorrowingRecordId = borrowingRecordId;
        BookId = bookId;
        BookTitle = bookTitle;
        MemberId = memberId;
        MemberName = memberName;
        ReturnDate = returnDate;
    }
}
