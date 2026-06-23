using System.Text.Json.Serialization;

namespace DotNetModulith.Abstractions.Events;

public sealed record BookOverdueIntegrationEvent : IntegrationEvent
{
    public Guid BorrowingRecordId { get; init; }
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public Guid MemberId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public DateOnly DueDate { get; init; }
    public int OverdueDays { get; init; }

    [JsonConstructor]
    public BookOverdueIntegrationEvent(
        Guid borrowingRecordId,
        Guid bookId,
        string bookTitle,
        Guid memberId,
        string memberName,
        DateOnly dueDate,
        int overdueDays)
    {
        BorrowingRecordId = borrowingRecordId;
        BookId = bookId;
        BookTitle = bookTitle;
        MemberId = memberId;
        MemberName = memberName;
        DueDate = dueDate;
        OverdueDays = overdueDays;
    }
}
