using System.Text.Json.Serialization;

namespace DotNetModulith.Abstractions.Events;

public sealed record ReservationAvailableIntegrationEvent : IntegrationEvent
{
    public Guid ReservationId { get; init; }
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public Guid MemberId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public DateOnly ExpiryDate { get; init; }

    [JsonConstructor]
    public ReservationAvailableIntegrationEvent(
        Guid reservationId,
        Guid bookId,
        string bookTitle,
        Guid memberId,
        string memberName,
        DateOnly expiryDate)
    {
        ReservationId = reservationId;
        BookId = bookId;
        BookTitle = bookTitle;
        MemberId = memberId;
        MemberName = memberName;
        ExpiryDate = expiryDate;
    }
}
