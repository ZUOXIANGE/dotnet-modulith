using System.Text.Json.Serialization;

namespace DotNetModulith.Abstractions.Events;

public sealed record FineCreatedIntegrationEvent : IntegrationEvent
{
    public Guid FineId { get; init; }
    public Guid MemberId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public Guid BorrowingRecordId { get; init; }
    public decimal Amount { get; init; }
    public string Reason { get; init; } = string.Empty;

    [JsonConstructor]
    public FineCreatedIntegrationEvent(
        Guid fineId,
        Guid memberId,
        string memberName,
        Guid borrowingRecordId,
        decimal amount,
        string reason)
    {
        FineId = fineId;
        MemberId = memberId;
        MemberName = memberName;
        BorrowingRecordId = borrowingRecordId;
        Amount = amount;
        Reason = reason;
    }
}
