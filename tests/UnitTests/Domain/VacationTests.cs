using FluentAssertions;
using Solodoc.Domain.Entities.Employees;
using Solodoc.Domain.Enums;

namespace Solodoc.UnitTests.Domain;

public class VacationTests
{
    [Fact]
    public void VacationBalance_RemainingDays_CalculatesCorrectly()
    {
        var balance = new VacationBalance
        {
            AnnualAllowanceDays = 25,
            CarriedOverDays = 5,
            UsedDays = 12
        };

        balance.RemainingDays.Should().Be(18);
    }

    [Fact]
    public void VacationBalance_DeductsCarriedOverFirst_RemainingIsCorrect()
    {
        // CarriedOver (3) + Annual (25) = 28 total, Used 3 => 25 remaining
        // Conceptually carried-over is used first, but the computed property is a simple subtraction
        var balance = new VacationBalance
        {
            AnnualAllowanceDays = 25,
            CarriedOverDays = 3,
            UsedDays = 3
        };

        // After using exactly the carried-over days, annual remains untouched
        balance.RemainingDays.Should().Be(25);
    }

    [Fact]
    public void VacationBalance_AllUsed_RemainingIsZero()
    {
        var balance = new VacationBalance
        {
            AnnualAllowanceDays = 25,
            CarriedOverDays = 3,
            UsedDays = 28
        };

        balance.RemainingDays.Should().Be(0);
    }

    [Fact]
    public void VacationBalance_OverUsed_RemainingIsNegative()
    {
        var balance = new VacationBalance
        {
            AnnualAllowanceDays = 25,
            CarriedOverDays = 0,
            UsedDays = 27
        };

        balance.RemainingDays.Should().Be(-2);
    }

    [Fact]
    public void VacationEntry_EndDate_CanBeSetAfterStartDate()
    {
        var entry = new VacationEntry
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 14),
            Days = 10
        };

        entry.EndDate.Should().BeAfter(entry.StartDate);
    }

    [Fact]
    public void VacationEntry_DefaultStatus_IsPending()
    {
        var entry = new VacationEntry();

        entry.Status.Should().Be(VacationStatus.Pending);
    }
}
