using DotNetModulith.Modules.Books.Domain;
using FluentAssertions;
using Xunit;

namespace DotNetModulith.UnitTests;

public class BookEntityTests
{
    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            "A handbook of agile software craftsmanship",
            Guid.NewGuid(),
            5,
            "https://example.com/cover.jpg",
            now);

        entity.Id.Should().NotBeEmpty();
        entity.Isbn.Should().Be("978-0-13-235088-4");
        entity.Title.Should().Be("Clean Code");
        entity.Author.Should().Be("Robert C. Martin");
        entity.Publisher.Should().Be("Prentice Hall");
        entity.PublishDate.Should().Be(new DateOnly(2008, 8, 1));
        entity.Description.Should().Be("A handbook of agile software craftsmanship");
        entity.TotalCopies.Should().Be(5);
        entity.AvailableCopies.Should().Be(5);
        entity.CoverImageUrl.Should().Be("https://example.com/cover.jpg");
        entity.Status.Should().Be(BookStatus.Available);
        entity.CreatedAt.Should().Be(now);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Create_WithZeroCopies_ShouldSetStatusToOutOfStock()
    {
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            Guid.NewGuid(),
            0,
            string.Empty,
            DateTimeOffset.UtcNow);

        entity.Status.Should().Be(BookStatus.OutOfStock);
        entity.AvailableCopies.Should().Be(0);
    }

    [Fact]
    public void UpdateInfo_ShouldUpdateAllProperties()
    {
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            Guid.NewGuid(),
            3,
            string.Empty,
            DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        var newCategoryId = Guid.NewGuid();
        entity.UpdateInfo(
            "978-0-13-468599-1",
            "Clean Architecture",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2017, 9, 10),
            "A Craftsman's Guide to Software Structure and Design",
            newCategoryId,
            10,
            "https://example.com/cover2.jpg",
            now);

        entity.Isbn.Should().Be("978-0-13-468599-1");
        entity.Title.Should().Be("Clean Architecture");
        entity.TotalCopies.Should().Be(10);
        entity.CategoryId.Should().Be(newCategoryId);
        entity.Status.Should().Be(BookStatus.Available);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void UpdateInfo_WithZeroCopies_ShouldSetStatusToOutOfStock()
    {
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            Guid.NewGuid(),
            3,
            string.Empty,
            DateTimeOffset.UtcNow);

        entity.UpdateInfo(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            entity.CategoryId,
            0,
            string.Empty,
            DateTimeOffset.UtcNow);

        entity.Status.Should().Be(BookStatus.OutOfStock);
    }

    [Fact]
    public void BorrowCopy_ShouldDecrementAvailableCopies()
    {
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            Guid.NewGuid(),
            3,
            string.Empty,
            DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.BorrowCopy(now);

        entity.AvailableCopies.Should().Be(2);
        entity.Status.Should().Be(BookStatus.Available);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void BorrowCopy_LastCopy_ShouldSetStatusToOutOfStock()
    {
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            Guid.NewGuid(),
            1,
            string.Empty,
            DateTimeOffset.UtcNow);

        entity.BorrowCopy(DateTimeOffset.UtcNow);

        entity.AvailableCopies.Should().Be(0);
        entity.Status.Should().Be(BookStatus.OutOfStock);
    }

    [Fact]
    public void BorrowCopy_WhenNoAvailableCopies_ShouldThrow()
    {
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            Guid.NewGuid(),
            0,
            string.Empty,
            DateTimeOffset.UtcNow);

        var act = () => entity.BorrowCopy(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No available copies to borrow");
    }

    [Fact]
    public void ReturnCopy_ShouldIncrementAvailableCopies()
    {
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            Guid.NewGuid(),
            3,
            string.Empty,
            DateTimeOffset.UtcNow);

        entity.BorrowCopy(DateTimeOffset.UtcNow);
        entity.BorrowCopy(DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.ReturnCopy(now);

        entity.AvailableCopies.Should().Be(2);
        entity.Status.Should().Be(BookStatus.Available);
        entity.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void ReturnCopy_WhenAllReturned_ShouldThrow()
    {
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            Guid.NewGuid(),
            3,
            string.Empty,
            DateTimeOffset.UtcNow);

        var act = () => entity.ReturnCopy(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("All copies are already returned");
    }

    [Fact]
    public void ReturnCopy_FromOutOfStock_ShouldRestoreAvailableStatus()
    {
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            Guid.NewGuid(),
            1,
            string.Empty,
            DateTimeOffset.UtcNow);

        entity.BorrowCopy(DateTimeOffset.UtcNow);
        entity.Status.Should().Be(BookStatus.OutOfStock);

        entity.ReturnCopy(DateTimeOffset.UtcNow);

        entity.Status.Should().Be(BookStatus.Available);
        entity.AvailableCopies.Should().Be(1);
    }

    [Fact]
    public void SetStatus_ShouldUpdateStatus()
    {
        var entity = BookEntity.Create(
            "978-0-13-235088-4",
            "Clean Code",
            "Robert C. Martin",
            "Prentice Hall",
            new DateOnly(2008, 8, 1),
            string.Empty,
            Guid.NewGuid(),
            3,
            string.Empty,
            DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.SetStatus(BookStatus.Damaged, now);

        entity.Status.Should().Be(BookStatus.Damaged);
        entity.UpdatedAt.Should().Be(now);
    }
}
