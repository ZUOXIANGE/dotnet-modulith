using DotNetModulith.Modules.Borrowing.Domain;
using FluentAssertions;
using Xunit;

namespace DotNetModulith.UnitTests;

public class BorrowingRecordEntityTests
{
    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var bookId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var borrowDate = new DateOnly(2026, 6, 1);
        var dueDate = new DateOnly(2026, 6, 15);

        var entity = BorrowingRecordEntity.Create(bookId, memberId, borrowDate, dueDate, now);

        entity.Id.Should().NotBeEmpty();
        entity.BookId.Should().Be(bookId);
        entity.MemberId.Should().Be(memberId);
        entity.BorrowDate.Should().Be(borrowDate);
        entity.DueDate.Should().Be(dueDate);
        entity.ReturnDate.Should().BeNull();
        entity.Status.Should().Be(BorrowingStatus.Borrowed);
        entity.RenewalCount.Should().Be(0);
        entity.Notes.Should().BeEmpty();
        entity.CreatedAt.Should().Be(now);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Return_ShouldSetStatusToReturned()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        var returnDate = new DateOnly(2026, 6, 10);
        var now = DateTimeOffset.UtcNow;
        entity.Return(returnDate, now);

        entity.Status.Should().Be(BorrowingStatus.Returned);
        entity.ReturnDate.Should().Be(returnDate);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Return_WhenAlreadyReturned_ShouldThrow()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        entity.Return(new DateOnly(2026, 6, 10), DateTimeOffset.UtcNow);

        var act = () => entity.Return(new DateOnly(2026, 6, 12), DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only borrowed or overdue records can be returned");
    }

    [Fact]
    public void Return_WhenLost_ShouldThrow()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        entity.MarkLost(DateTimeOffset.UtcNow);

        var act = () => entity.Return(new DateOnly(2026, 6, 12), DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only borrowed or overdue records can be returned");
    }

    [Fact]
    public void Return_WhenOverdue_ShouldSetStatusToReturned()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        entity.MarkOverdue(new DateOnly(2026, 6, 20), DateTimeOffset.UtcNow);
        entity.Status.Should().Be(BorrowingStatus.Overdue);

        entity.Return(new DateOnly(2026, 6, 22), DateTimeOffset.UtcNow);

        entity.Status.Should().Be(BorrowingStatus.Returned);
    }

    [Fact]
    public void Renew_ShouldIncrementCountAndUpdateDueDate()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        var newDueDate = new DateOnly(2026, 6, 30);
        var now = DateTimeOffset.UtcNow;
        entity.Renew(newDueDate, now);

        entity.RenewalCount.Should().Be(1);
        entity.DueDate.Should().Be(newDueDate);
        entity.Status.Should().Be(BorrowingStatus.Borrowed);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Renew_ThreeTimes_ShouldBeAllowed()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        entity.Renew(new DateOnly(2026, 6, 30), DateTimeOffset.UtcNow);
        entity.Renew(new DateOnly(2026, 7, 15), DateTimeOffset.UtcNow);
        entity.Renew(new DateOnly(2026, 7, 30), DateTimeOffset.UtcNow);

        entity.RenewalCount.Should().Be(3);
    }

    [Fact]
    public void Renew_FourthTime_ShouldThrow()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        entity.Renew(new DateOnly(2026, 6, 30), DateTimeOffset.UtcNow);
        entity.Renew(new DateOnly(2026, 7, 15), DateTimeOffset.UtcNow);
        entity.Renew(new DateOnly(2026, 7, 30), DateTimeOffset.UtcNow);

        var act = () => entity.Renew(new DateOnly(2026, 8, 15), DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Maximum renewal count reached");
    }

    [Fact]
    public void Renew_WhenReturned_ShouldThrow()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        entity.Return(new DateOnly(2026, 6, 10), DateTimeOffset.UtcNow);

        var act = () => entity.Renew(new DateOnly(2026, 6, 30), DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only borrowed or overdue records can be renewed");
    }

    [Fact]
    public void MarkLost_ShouldSetStatusToLost()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.MarkLost(now);

        entity.Status.Should().Be(BorrowingStatus.Lost);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void MarkLost_WhenAlreadyReturned_ShouldThrow()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        entity.Return(new DateOnly(2026, 6, 10), DateTimeOffset.UtcNow);

        var act = () => entity.MarkLost(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only borrowed or overdue records can be marked as lost");
    }

    [Fact]
    public void MarkOverdue_WhenPastDueDate_ShouldSetStatusToOverdue()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        var today = new DateOnly(2026, 6, 20);
        var now = DateTimeOffset.UtcNow;
        entity.MarkOverdue(today, now);

        entity.Status.Should().Be(BorrowingStatus.Overdue);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void MarkOverdue_WhenNotPastDueDate_ShouldNotChangeStatus()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        var today = new DateOnly(2026, 6, 15);
        entity.MarkOverdue(today, DateTimeOffset.UtcNow);

        entity.Status.Should().Be(BorrowingStatus.Borrowed);
    }

    [Fact]
    public void MarkOverdue_WhenAlreadyReturned_ShouldNotChangeStatus()
    {
        var entity = BorrowingRecordEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15),
            DateTimeOffset.UtcNow);

        entity.Return(new DateOnly(2026, 6, 10), DateTimeOffset.UtcNow);

        var today = new DateOnly(2026, 6, 20);
        entity.MarkOverdue(today, DateTimeOffset.UtcNow);

        entity.Status.Should().Be(BorrowingStatus.Returned);
    }
}
