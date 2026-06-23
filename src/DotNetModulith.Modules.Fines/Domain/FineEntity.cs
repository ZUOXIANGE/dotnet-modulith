namespace DotNetModulith.Modules.Fines.Domain;

public sealed class FineEntity
{
    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid? BorrowingRecordId { get; private set; }
    public decimal Amount { get; private set; }
    public FineReason Reason { get; private set; }
    public FineStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private FineEntity()
    {
    }

    public static FineEntity Create(
        Guid memberId,
        Guid? borrowingRecordId,
        decimal amount,
        FineReason reason,
        DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            BorrowingRecordId = borrowingRecordId,
            Amount = amount,
            Reason = reason,
            Status = FineStatus.Unpaid,
            CreatedAt = now,
            PaidAt = null,
            UpdatedAt = now
        };

    public void Pay(DateTimeOffset now)
    {
        if (Status != FineStatus.Unpaid)
            throw new InvalidOperationException("Only unpaid fines can be paid");

        Status = FineStatus.Paid;
        PaidAt = now;
        UpdatedAt = now;
    }

    public void Waive(DateTimeOffset now)
    {
        if (Status != FineStatus.Unpaid)
            throw new InvalidOperationException("Only unpaid fines can be waived");

        Status = FineStatus.Waived;
        UpdatedAt = now;
    }
}
