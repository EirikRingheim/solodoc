using FluentAssertions;
using Solodoc.Domain.Entities.Hours;

namespace Solodoc.UnitTests.Services;

public class OvertimeCalculationTests
{
    private const decimal NormalDayHours = 7.5m;

    private static decimal CalculateOvertime(decimal totalHours)
    {
        return totalHours > NormalDayHours ? totalHours - NormalDayHours : 0;
    }

    [Fact]
    public void Calculate_NormalDay_NoOvertime()
    {
        var entry = new TimeEntry
        {
            Hours = 7.5m,
            OvertimeHours = CalculateOvertime(7.5m)
        };

        entry.OvertimeHours.Should().Be(0);
    }

    [Fact]
    public void Calculate_LongDay_HasOvertime()
    {
        var totalHours = 10.5m;
        var entry = new TimeEntry
        {
            Hours = totalHours,
            OvertimeHours = CalculateOvertime(totalHours)
        };

        entry.OvertimeHours.Should().Be(3.0m);
    }

    [Fact]
    public void Calculate_HalfDay_NoOvertime()
    {
        var entry = new TimeEntry
        {
            Hours = 3.75m,
            OvertimeHours = CalculateOvertime(3.75m)
        };

        entry.OvertimeHours.Should().Be(0);
    }

    [Fact]
    public void Calculate_ExactNormalDay_NoOvertime()
    {
        var entry = new TimeEntry
        {
            Hours = NormalDayHours,
            OvertimeHours = CalculateOvertime(NormalDayHours)
        };

        entry.OvertimeHours.Should().Be(0);
    }

    [Fact]
    public void Calculate_SlightlyOverNormal_HasSmallOvertime()
    {
        var totalHours = 8.0m;
        var entry = new TimeEntry
        {
            Hours = totalHours,
            OvertimeHours = CalculateOvertime(totalHours)
        };

        entry.OvertimeHours.Should().Be(0.5m);
    }

    [Fact]
    public void TimeEntry_WithBreak_ReducesEffectiveHours()
    {
        var entry = new TimeEntry
        {
            Hours = 8.0m,
            BreakMinutes = 30
        };

        var effectiveHours = entry.Hours - (entry.BreakMinutes / 60.0m);
        effectiveHours.Should().Be(7.5m);
    }
}
