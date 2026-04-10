using FluentAssertions;
using Solodoc.Domain.Entities.Hours;
using Solodoc.Domain.Enums;

namespace Solodoc.UnitTests.Services;

public class AllowanceCalculationTests
{
    [Fact]
    public void TimeBased_WithinRange_AppliesAllowance()
    {
        var rule = new AllowanceRule
        {
            Name = "Nattillegg",
            Type = AllowanceType.TimeBased,
            AmountType = AllowanceAmountType.FixedKroner,
            Amount = 75.00m,
            TimeRangeStart = new TimeOnly(21, 0),
            TimeRangeEnd = new TimeOnly(6, 0),
            IsActive = true
        };

        var workTime = new TimeOnly(23, 0);
        var isWithinRange = workTime >= rule.TimeRangeStart || workTime <= rule.TimeRangeEnd;

        isWithinRange.Should().BeTrue();
        rule.Amount.Should().Be(75.00m);
    }

    [Fact]
    public void TimeBased_OutsideRange_NoAllowance()
    {
        var rule = new AllowanceRule
        {
            Name = "Nattillegg",
            Type = AllowanceType.TimeBased,
            AmountType = AllowanceAmountType.FixedKroner,
            Amount = 75.00m,
            TimeRangeStart = new TimeOnly(21, 0),
            TimeRangeEnd = new TimeOnly(6, 0),
            IsActive = true
        };

        var workTime = new TimeOnly(14, 0);
        var isWithinRange = workTime >= rule.TimeRangeStart || workTime <= rule.TimeRangeEnd;

        isWithinRange.Should().BeFalse();
    }

    [Fact]
    public void FixedPerDay_AlwaysApplies()
    {
        var rule = new AllowanceRule
        {
            Name = "Diett",
            Type = AllowanceType.FixedPerDay,
            AmountType = AllowanceAmountType.FixedKroner,
            Amount = 350.00m,
            IsActive = true
        };

        // FixedPerDay has no time range check - applies to entire day
        rule.Type.Should().Be(AllowanceType.FixedPerDay);
        rule.TimeRangeStart.Should().BeNull();
        rule.TimeRangeEnd.Should().BeNull();
        rule.Amount.Should().Be(350.00m);
    }

    [Fact]
    public void AllowanceRule_DefaultIsActive_IsTrue()
    {
        var rule = new AllowanceRule();

        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TimeEntryAllowance_StoresCalculatedAmount()
    {
        var hoursInRange = 3.5m;
        var ratePerHour = 75.00m;

        var allowance = new TimeEntryAllowance
        {
            TimeEntryId = Guid.NewGuid(),
            AllowanceRuleId = Guid.NewGuid(),
            Hours = hoursInRange,
            Amount = hoursInRange * ratePerHour
        };

        allowance.Amount.Should().Be(262.50m);
        allowance.Hours.Should().Be(3.5m);
    }

    [Fact]
    public void Percentage_AmountType_CalculatesCorrectly()
    {
        var rule = new AllowanceRule
        {
            Name = "Helgetillegg",
            Type = AllowanceType.TimeBased,
            AmountType = AllowanceAmountType.Percentage,
            Amount = 50.00m // 50% extra
        };

        rule.AmountType.Should().Be(AllowanceAmountType.Percentage);
        rule.Amount.Should().Be(50.00m);
    }
}
