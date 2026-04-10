using FluentAssertions;
using Solodoc.Domain.Entities.Employees;
using Solodoc.Domain.Enums;

namespace Solodoc.UnitTests.Domain;

public class EmployeeTests
{
    [Fact]
    public void Certification_IsExpired_WhenExpiryDatePassed()
    {
        var cert = new EmployeeCertification
        {
            Name = "HMS-kort",
            ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1))
        };

        cert.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void Certification_IsNotExpired_WhenExpiryInFuture()
    {
        var cert = new EmployeeCertification
        {
            Name = "HMS-kort",
            ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(60))
        };

        cert.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Certification_IsExpiringSoon_WhenWithin30Days()
    {
        var cert = new EmployeeCertification
        {
            Name = "Varmearbeid",
            ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(15))
        };

        cert.IsExpiringSoon.Should().BeTrue();
        cert.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Certification_IsNotExpiringSoon_WhenOver30Days()
    {
        var cert = new EmployeeCertification
        {
            Name = "Varmearbeid",
            ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(60))
        };

        cert.IsExpiringSoon.Should().BeFalse();
    }

    [Fact]
    public void VacationBalance_CalculatesRemaining()
    {
        var balance = new VacationBalance
        {
            AnnualAllowanceDays = 25,
            CarriedOverDays = 3,
            UsedDays = 10
        };

        balance.RemainingDays.Should().Be(18);
    }

    [Fact]
    public void VacationBalance_DeductsCarriedOverFirst()
    {
        // With 3 carried over + 25 annual = 28 total
        // Using 5 days: remaining should be 23
        var balance = new VacationBalance
        {
            AnnualAllowanceDays = 25,
            CarriedOverDays = 3,
            UsedDays = 5
        };

        balance.RemainingDays.Should().Be(23);
    }

    [Fact]
    public void VacationEntry_DefaultStatus_IsPending()
    {
        var entry = new VacationEntry();
        entry.Status.Should().Be(VacationStatus.Pending);
    }
}
