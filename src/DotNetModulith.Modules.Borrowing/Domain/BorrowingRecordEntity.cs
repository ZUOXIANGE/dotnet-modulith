namespace DotNetModulith.Modules.Borrowing.Domain;

public sealed class BorrowingRecordEntity
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public Guid MemberId { get; private set; }
    public DateOnly BorrowDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateOnly? ReturnDate { get; private set; }
    public BorrowingStatus Status { get; private set; }
    public int RenewalCount { get; private set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private BorrowingRecordEntity()
    {
    }

    public static BorrowingRecordEntity Create(
        Guid bookId,
        Guid memberId,
        DateOnly borrowDate,
        DateOnly dueDate,
        DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            MemberId = memberId,
            BorrowDate = borrowDate,
            DueDate = dueDate,
            ReturnDate = null,
            Status = BorrowingStatus.Borrowed,
            RenewalCount = 0,
            Notes = string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Return(DateOnly returnDate, DateTimeOffset now)
    {
        if (Status != BorrowingStatus.Borrowed && Status != BorrowingStatus.Overdue)
            throw new InvalidOperationException("Only borrowed or overdue records can be returned");

        ReturnDate = returnDate;
        Status = BorrowingStatus.Returned;
        UpdatedAt = now;
    }

    public void Renew(DateOnly newDueDate, DateTimeOffset now)
    {
        if (Status != BorrowingStatus.Borrowed && Status != BorrowingStatus.Overdue)
            throw new InvalidOperationException("Only borrowed or overdue records can be renewed");

        if (RenewalCount >= 3)
            throw new InvalidOperationException("Maximum renewal count reached");

        RenewalCount++;
        DueDate = newDueDate;
        Status = BorrowingStatus.Borrowed;
        UpdatedAt = now;
    }

    public void MarkLost(DateTimeOffset now)
    {
        if (Status != BorrowingStatus.Borrowed && Status != BorrowingStatus.Overdue)
            throw new InvalidOperationException("Only borrowed or overdue records can be marked as lost");

        Status = BorrowingStatus.Lost;
        UpdatedAt = now;
    }

    public void MarkOverdue(DateOnly today, DateTimeOffset now)
    {
        if (Status == BorrowingStatus.Borrowed && today > DueDate)
        {
            Status = BorrowingStatus.Overdue;
            UpdatedAt = now;
        }
    }
}
