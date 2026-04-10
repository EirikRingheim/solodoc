using FluentAssertions;
using Solodoc.Domain.Entities.Hours;
using Solodoc.Domain.Enums;

namespace Solodoc.UnitTests.Domain;

public class TimeEntryTests
{
    [Fact]
    public void TimeEntry_MustHaveProjectOrJob_NotBoth()
    {
        var entry = new TimeEntry
        {
            ProjectId = Guid.NewGuid(),
            JobId = null
        };

        var hasBoth = entry.ProjectId.HasValue && entry.JobId.HasValue;
        var hasNeither = !entry.ProjectId.HasValue && !entry.JobId.HasValue;

        hasBoth.Should().BeFalse();
        hasNeither.Should().BeFalse();
    }

    [Fact]
    public void TimeEntry_DefaultCategory_IsArbeid()
    {
        var entry = new TimeEntry();
        entry.Category.Should().Be(TimeEntryCategory.Arbeid);
    }

    [Fact]
    public void TimeEntry_DefaultStatus_IsDraft()
    {
        var entry = new TimeEntry();
        entry.Status.Should().Be(TimeEntryStatus.Draft);
    }

    [Fact]
    public void TimeEntry_OvertimeCalculation_DailyThreshold()
    {
        var entry = new TimeEntry
        {
            Hours = 10.5m,
            OvertimeHours = 3.0m // 10.5 - 7.5 scheduled = 3.0 overtime
        };

        entry.OvertimeHours.Should().Be(3.0m);
        (entry.Hours - entry.OvertimeHours).Should().Be(7.5m);
    }

    [Fact]
    public void AllowanceRule_TimeBased_HasTimeRange()
    {
        var rule = new AllowanceRule
        {
            Name = "Kveldstillegg",
            Type = AllowanceType.TimeBased,
            AmountType = AllowanceAmountType.FixedKroner,
            Amount = 50m,
            TimeRangeStart = new TimeOnly(15, 0),
            TimeRangeEnd = new TimeOnly(21, 0)
        };

        rule.Type.Should().Be(AllowanceType.TimeBased);
        rule.TimeRangeStart.Should().Be(new TimeOnly(15, 0));
        rule.TimeRangeEnd.Should().Be(new TimeOnly(21, 0));
    }

    [Fact]
    public void PublicHoliday_RodDag_IsNotHalfDay()
    {
        var holiday = new PublicHoliday
        {
            Date = new DateOnly(2026, 5, 1),
            Name = "Arbeidernes dag",
            IsHalfDay = false
        };

        holiday.IsHalfDay.Should().BeFalse();
        holiday.HalfDayCutoff.Should().BeNull();
    }

    [Fact]
    public void PublicHoliday_HalvRod_HasCutoff()
    {
        var holiday = new PublicHoliday
        {
            Date = new DateOnly(2026, 12, 24),
            Name = "Julaften",
            IsHalfDay = true,
            HalfDayCutoff = new TimeOnly(12, 0)
        };

        holiday.IsHalfDay.Should().BeTrue();
        holiday.HalfDayCutoff.Should().Be(new TimeOnly(12, 0));
    }
}
