using DotNetModulith.Modules.Fines.Domain;
using FluentAssertions;
using Xunit;

namespace DotNetModulith.UnitTests;

public class FineEntityTests
{
    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var memberId = Guid.NewGuid();
        var borrowingRecordId = Guid.NewGuid();
        var amount = 50.00m;

        var entity = FineEntity.Create(memberId, borrowingRecordId, amount, FineReason.Overdue, now);

        entity.Id.Should().NotBeEmpty();
        entity.MemberId.Should().Be(memberId);
        entity.BorrowingRecordId.Should().Be(borrowingRecordId);
        entity.Amount.Should().Be(amount);
        entity.Reason.Should().Be(FineReason.Overdue);
        entity.Status.Should().Be(FineStatus.Unpaid);
        entity.PaidAt.Should().BeNull();
        entity.CreatedAt.Should().Be(now);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Create_WithNullBorrowingRecordId_ShouldBeAllowed()
    {
        var entity = FineEntity.Create(Guid.NewGuid(), null, 100.00m, FineReason.Damaged, DateTimeOffset.UtcNow);

        entity.BorrowingRecordId.Should().BeNull();
        entity.Status.Should().Be(FineStatus.Unpaid);
    }

    [Fact]
    public void Pay_ShouldSetStatusToPaid()
    {
        var entity = FineEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 50.00m, FineReason.Overdue, DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.Pay(now);

        entity.Status.Should().Be(FineStatus.Paid);
        entity.PaidAt.Should().Be(now);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Pay_WhenAlreadyPaid_ShouldThrow()
    {
        var entity = FineEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 50.00m, FineReason.Overdue, DateTimeOffset.UtcNow);

        entity.Pay(DateTimeOffset.UtcNow);

        var act = () => entity.Pay(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only unpaid fines can be paid");
    }

    [Fact]
    public void Pay_WhenWaived_ShouldThrow()
    {
        var entity = FineEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 50.00m, FineReason.Overdue, DateTimeOffset.UtcNow);

        entity.Waive(DateTimeOffset.UtcNow);

        var act = () => entity.Pay(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only unpaid fines can be paid");
    }

    [Fact]
    public void Waive_ShouldSetStatusToWaived()
    {
        var entity = FineEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 50.00m, FineReason.Overdue, DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.Waive(now);

        entity.Status.Should().Be(FineStatus.Waived);
        entity.PaidAt.Should().BeNull();
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Waive_WhenAlreadyPaid_ShouldThrow()
    {
        var entity = FineEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 50.00m, FineReason.Overdue, DateTimeOffset.UtcNow);

        entity.Pay(DateTimeOffset.UtcNow);

        var act = () => entity.Waive(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only unpaid fines can be waived");
    }

    [Fact]
    public void Waive_WhenAlreadyWaived_ShouldThrow()
    {
        var entity = FineEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 50.00m, FineReason.Overdue, DateTimeOffset.UtcNow);

        entity.Waive(DateTimeOffset.UtcNow);

        var act = () => entity.Waive(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only unpaid fines can be waived");
    }
}
