namespace DotNetModulith.Modules.Members.Domain;

public sealed class MemberEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public MembershipType MembershipType { get; private set; }
    public MemberStatus Status { get; private set; }
    public DateOnly JoinDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public int MaxBorrowCount { get; private set; }
    public int CurrentBorrowCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private MemberEntity() { }

    public static MemberEntity Create(
        string name,
        string phone,
        string email,
        string address,
        MembershipType membershipType,
        DateOnly joinDate,
        DateOnly? expiryDate,
        DateTimeOffset now)
    {
        var maxBorrowCount = membershipType switch
        {
            MembershipType.Vip => 10,
            MembershipType.Teacher => 8,
            MembershipType.Student => 5,
            _ => 5
        };

        return new MemberEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Phone = phone,
            Email = email,
            Address = address,
            MembershipType = membershipType,
            Status = MemberStatus.Active,
            JoinDate = joinDate,
            ExpiryDate = expiryDate,
            MaxBorrowCount = maxBorrowCount,
            CurrentBorrowCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string phone, string email, string address, MembershipType membershipType, DateOnly? expiryDate, DateTimeOffset now)
    {
        Name = name;
        Phone = phone;
        Email = email;
        Address = address;
        MembershipType = membershipType;
        ExpiryDate = expiryDate;
        MaxBorrowCount = membershipType switch
        {
            MembershipType.Vip => 10,
            MembershipType.Teacher => 8,
            MembershipType.Student => 5,
            _ => 5
        };
        UpdatedAt = now;
    }

    public void IncrementBorrowCount(DateTimeOffset now)
    {
        if (CurrentBorrowCount >= MaxBorrowCount)
            throw new InvalidOperationException("member has reached maximum borrow limit");

        CurrentBorrowCount++;
        UpdatedAt = now;
    }

    public void DecrementBorrowCount(DateTimeOffset now)
    {
        if (CurrentBorrowCount <= 0)
            throw new InvalidOperationException("member has no borrowed books");

        CurrentBorrowCount--;
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        if (Status == MemberStatus.Cancelled)
            throw new InvalidOperationException("cannot suspend a cancelled member");

        Status = MemberStatus.Suspended;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        Status = MemberStatus.Active;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (CurrentBorrowCount > 0)
            throw new InvalidOperationException("cannot cancel a member with borrowed books");

        Status = MemberStatus.Cancelled;
        UpdatedAt = now;
    }

    public void CheckAndUpdateExpiry(DateOnly today, DateTimeOffset now)
    {
        if (ExpiryDate.HasValue && today > ExpiryDate.Value && Status == MemberStatus.Active)
        {
            Status = MemberStatus.Expired;
            UpdatedAt = now;
        }
    }
}
