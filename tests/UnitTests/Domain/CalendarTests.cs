using FluentAssertions;
using Solodoc.Domain.Entities.Calendar;

namespace Solodoc.UnitTests.Domain;

public class CalendarTests
{
    [Fact]
    public void CalendarEvent_AllDay_EndAtCanBeNull()
    {
        var ev = new CalendarEvent
        {
            Title = "HMS Day",
            IsAllDay = true,
            StartAt = DateTimeOffset.UtcNow.Date
        };

        ev.IsAllDay.Should().BeTrue();
        ev.EndAt.Should().BeNull();
    }

    [Fact]
    public void CalendarEvent_DefaultIsAllDay_IsFalse()
    {
        var ev = new CalendarEvent();

        ev.IsAllDay.Should().BeFalse();
    }

    [Fact]
    public void CalendarEvent_TimedEvent_HasEndAt()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(2);

        var ev = new CalendarEvent
        {
            Title = "Project meeting",
            StartAt = start,
            EndAt = end,
            IsAllDay = false
        };

        ev.EndAt.Should().NotBeNull();
        ev.EndAt.Should().BeAfter(ev.StartAt);
    }

    [Fact]
    public void CalendarEvent_DefaultInvitations_IsEmpty()
    {
        var ev = new CalendarEvent();

        ev.Invitations.Should().BeEmpty();
    }
}
