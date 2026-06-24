using DotNetModulith.Modules.Notifications.Domain;
using FluentAssertions;
using Xunit;

namespace DotNetModulith.UnitTests;

public class NotificationEntityTests
{
    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var entity = NotificationEntity.Create(
            "Borrowing Reminder",
            "Your book is due tomorrow",
            NotificationType.BorrowDue,
            "user-123",
            now);

        entity.Id.Should().NotBeEmpty();
        entity.Title.Should().Be("Borrowing Reminder");
        entity.Content.Should().Be("Your book is due tomorrow");
        entity.Type.Should().Be(NotificationType.BorrowDue);
        entity.RecipientId.Should().Be("user-123");
        entity.IsRead.Should().BeFalse();
        entity.ReadAt.Should().BeNull();
        entity.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void MarkAsRead_ShouldSetIsReadAndReadAt()
    {
        var entity = NotificationEntity.Create(
            "Test",
            "Content",
            NotificationType.System,
            "user-123",
            DateTimeOffset.UtcNow);

        var now = DateTimeOffset.UtcNow;
        entity.MarkAsRead(now);

        entity.IsRead.Should().BeTrue();
        entity.ReadAt.Should().Be(now);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_ShouldNotChangeReadAt()
    {
        var entity = NotificationEntity.Create(
            "Test",
            "Content",
            NotificationType.System,
            "user-123",
            DateTimeOffset.UtcNow);

        var firstReadAt = DateTimeOffset.UtcNow;
        entity.MarkAsRead(firstReadAt);

        entity.MarkAsRead(DateTimeOffset.UtcNow.AddDays(1));

        entity.IsRead.Should().BeTrue();
        entity.ReadAt.Should().Be(firstReadAt);
    }
}
