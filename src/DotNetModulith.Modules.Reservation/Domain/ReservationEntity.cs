namespace DotNetModulith.Modules.Reservation.Domain;

public sealed class ReservationEntity
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public Guid MemberId { get; private set; }
    public DateOnly ReserveDate { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public ReservationStatus Status { get; private set; }
    public int QueuePosition { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ReservationEntity()
    {
    }

    public static ReservationEntity Create(
        Guid bookId,
        Guid memberId,
        DateOnly reserveDate,
        DateOnly expiryDate,
        int queuePosition,
        DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            MemberId = memberId,
            ReserveDate = reserveDate,
            ExpiryDate = expiryDate,
            Status = ReservationStatus.Pending,
            QueuePosition = queuePosition,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Fulfill(DateTimeOffset now)
    {
        if (Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Only pending reservations can be fulfilled");

        Status = ReservationStatus.Fulfilled;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Only pending reservations can be cancelled");

        Status = ReservationStatus.Cancelled;
        UpdatedAt = now;
    }

    public void MarkExpired(DateOnly today, DateTimeOffset now)
    {
        if (Status == ReservationStatus.Pending && today > ExpiryDate)
        {
            Status = ReservationStatus.Expired;
            UpdatedAt = now;
        }
    }
}
