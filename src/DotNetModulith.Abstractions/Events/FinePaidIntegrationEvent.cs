using System.Text.Json.Serialization;

namespace DotNetModulith.Abstractions.Events;

public sealed record FinePaidIntegrationEvent : IntegrationEvent
{
    public Guid FineId { get; init; }
    public Guid MemberId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public decimal Amount { get; init; }

    [JsonConstructor]
    public FinePaidIntegrationEvent(
        Guid fineId,
        Guid memberId,
        string memberName,
        decimal amount)
    {
        FineId = fineId;
        MemberId = memberId;
        MemberName = memberName;
        Amount = amount;
    }
}
