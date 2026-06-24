using DotNetModulith.Modules.Members.Domain;
using FluentAssertions;
using Xunit;

namespace DotNetModulith.UnitTests;

public class MemberEntityTests
{
    [Fact]
    public void Create_Student_ShouldHaveMaxBorrowCount5()
    {
        var now = DateTimeOffset.UtcNow;
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            now);

        entity.Id.Should().NotBeEmpty();
        entity.Name.Should().Be("Zhang San");
        entity.Phone.Should().Be("13800138000");
        entity.Email.Should().Be("zhangsan@example.com");
        entity.Address.Should().Be("Beijing");
        entity.MembershipType.Should().Be(MembershipType.Student);
        entity.Status.Should().Be(MemberStatus.Active);
        entity.JoinDate.Should().Be(new DateOnly(2026, 6, 1));
        entity.ExpiryDate.Should().Be(new DateOnly(2027, 6, 1));
        entity.MaxBorrowCount.Should().Be(5);
        entity.CurrentBorrowCount.Should().Be(0);
        entity.CreatedAt.Should().Be(now);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Create_Vip_ShouldHaveMaxBorrowCount10()
    {
        var entity = MemberEntity.Create(
            "Li Si",
            "13900139000",
            "lisi@example.com",
            "Shanghai",
            MembershipType.Vip,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1),
            DateTimeOffset.UtcNow);

        entity.MaxBorrowCount.Should().Be(10);
    }

    [Fact]
    public void Create_Teacher_ShouldHaveMaxBorrowCount8()
    {
        var entity = MemberEntity.Create(
            "Wang Wu",
            "13700137000",
            "wangwu@example.com",
            "Guangzhou",
            MembershipType.Teacher,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1),
            DateTimeOffset.UtcNow);

        entity.MaxBorrowCount.Should().Be(8);
    }

    [Fact]
    public void Update_ShouldUpdatePropertiesAndRecalculateMaxBorrowCount()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.Update(
            "Zhang San Updated",
            "13900139000",
            "zhangsan2@example.com",
            "Shanghai",
            MembershipType.Vip,
            new DateOnly(2028, 6, 1),
            now);

        entity.Name.Should().Be("Zhang San Updated");
        entity.Phone.Should().Be("13900139000");
        entity.Email.Should().Be("zhangsan2@example.com");
        entity.Address.Should().Be("Shanghai");
        entity.MembershipType.Should().Be(MembershipType.Vip);
        entity.ExpiryDate.Should().Be(new DateOnly(2028, 6, 1));
        entity.MaxBorrowCount.Should().Be(10);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void IncrementBorrowCount_ShouldIncrement()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Vip,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.IncrementBorrowCount(now);

        entity.CurrentBorrowCount.Should().Be(1);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void IncrementBorrowCount_WhenAtMax_ShouldThrow()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        for (var i = 0; i < 5; i++)
            entity.IncrementBorrowCount(DateTimeOffset.UtcNow);

        var act = () => entity.IncrementBorrowCount(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("member has reached maximum borrow limit");
    }

    [Fact]
    public void DecrementBorrowCount_ShouldDecrement()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        entity.IncrementBorrowCount(DateTimeOffset.UtcNow);
        entity.IncrementBorrowCount(DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.DecrementBorrowCount(now);

        entity.CurrentBorrowCount.Should().Be(1);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void DecrementBorrowCount_WhenZero_ShouldThrow()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        var act = () => entity.DecrementBorrowCount(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("member has no borrowed books");
    }

    [Fact]
    public void Suspend_ShouldSetStatusToSuspended()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.Suspend(now);

        entity.Status.Should().Be(MemberStatus.Suspended);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Suspend_WhenCancelled_ShouldThrow()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        entity.Cancel(DateTimeOffset.UtcNow);

        var act = () => entity.Suspend(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("cannot suspend a cancelled member");
    }

    [Fact]
    public void Activate_ShouldSetStatusToActive()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        entity.Suspend(DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.Activate(now);

        entity.Status.Should().Be(MemberStatus.Active);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Cancel_ShouldSetStatusToCancelled()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.Cancel(now);

        entity.Status.Should().Be(MemberStatus.Cancelled);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Cancel_WhenHasBorrowedBooks_ShouldThrow()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        entity.IncrementBorrowCount(DateTimeOffset.UtcNow);

        var act = () => entity.Cancel(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("cannot cancel a member with borrowed books");
    }

    [Fact]
    public void CheckAndUpdateExpiry_WhenExpired_ShouldSetStatusToExpired()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow);

        var today = new DateOnly(2027, 1, 1);
        var now = DateTimeOffset.UtcNow;
        entity.CheckAndUpdateExpiry(today, now);

        entity.Status.Should().Be(MemberStatus.Expired);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void CheckAndUpdateExpiry_WhenNotExpired_ShouldNotChangeStatus()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2027, 6, 1),
            DateTimeOffset.UtcNow);

        var today = new DateOnly(2026, 12, 31);
        entity.CheckAndUpdateExpiry(today, DateTimeOffset.UtcNow);

        entity.Status.Should().Be(MemberStatus.Active);
    }

    [Fact]
    public void CheckAndUpdateExpiry_WhenSuspended_ShouldNotChangeStatus()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 12, 31),
            DateTimeOffset.UtcNow);

        entity.Suspend(DateTimeOffset.UtcNow);

        var today = new DateOnly(2027, 1, 1);
        entity.CheckAndUpdateExpiry(today, DateTimeOffset.UtcNow);

        entity.Status.Should().Be(MemberStatus.Suspended);
    }

    [Fact]
    public void CheckAndUpdateExpiry_WithNoExpiryDate_ShouldNotChangeStatus()
    {
        var entity = MemberEntity.Create(
            "Zhang San",
            "13800138000",
            "zhangsan@example.com",
            "Beijing",
            MembershipType.Student,
            new DateOnly(2026, 6, 1),
            null,
            DateTimeOffset.UtcNow);

        entity.CheckAndUpdateExpiry(new DateOnly(2027, 1, 1), DateTimeOffset.UtcNow);

        entity.Status.Should().Be(MemberStatus.Active);
    }
}
