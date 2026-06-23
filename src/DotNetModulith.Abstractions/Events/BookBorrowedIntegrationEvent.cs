using System.Text.Json.Serialization;

namespace DotNetModulith.Abstractions.Events;

public sealed record BookBorrowedIntegrationEvent : IntegrationEvent
{
    public Guid BorrowingRecordId { get; init; }
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public Guid MemberId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public DateOnly DueDate { get; init; }

    [JsonConstructor]
    public BookBorrowedIntegrationEvent(
        Guid borrowingRecordId,
        Guid bookId,
        string bookTitle,
        Guid memberId,
        string memberName,
        DateOnly dueDate)
    {
        BorrowingRecordId = borrowingRecordId;
        BookId = bookId;
        BookTitle = bookTitle;
        MemberId = memberId;
        MemberName = memberName;
        DueDate = dueDate;
    }
}
