using FluentAssertions;
using Solodoc.Domain.Entities.Notifications;

namespace Solodoc.UnitTests.Domain;

public class NotificationTests
{
    [Fact]
    public void Notification_DefaultIsRead_IsFalse()
    {
        var notification = new Notification();

        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public void Notification_DefaultReadAt_IsNull()
    {
        var notification = new Notification();

        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void Notification_MarkRead_SetsIsReadAndReadAt()
    {
        var notification = new Notification
        {
            PersonId = Guid.NewGuid(),
            Title = "Nytt avvik rapportert"
        };

        notification.IsRead = true;
        notification.ReadAt = DateTimeOffset.UtcNow;

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public void Announcement_UrgencyLevel_DefaultsToOne()
    {
        var announcement = new Announcement();

        announcement.UrgencyLevel.Should().Be(1);
    }

    [Fact]
    public void Announcement_RequiresAcknowledgment_DefaultIsFalse()
    {
        var announcement = new Announcement();

        announcement.RequiresAcknowledgment.Should().BeFalse();
    }

    [Fact]
    public void Announcement_DefaultAcknowledgments_IsEmpty()
    {
        var announcement = new Announcement();

        announcement.Acknowledgments.Should().BeEmpty();
    }
}
