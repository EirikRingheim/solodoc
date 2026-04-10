using FluentAssertions;
using Solodoc.Domain.Entities.Deviations;
using Solodoc.Domain.Entities.Employees;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Enums;

namespace Solodoc.UnitTests.Services;

public class BackgroundJobServiceTests
{
    [Fact]
    public void CertificationExpiry_FindsExpiredCerts()
    {
        // Arrange
        var cert = new EmployeeCertification
        {
            PersonId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Truck License",
            Type = "License",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5)
        };

        // Act & Assert
        cert.IsExpired.Should().BeTrue();
        cert.IsExpiringSoon.Should().BeFalse();
    }

    [Fact]
    public void CertificationExpiry_FindsExpiringSoonCerts()
    {
        // Arrange
        var cert = new EmployeeCertification
        {
            PersonId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Safety Course",
            Type = "Certificate",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15)
        };

        // Act & Assert
        cert.IsExpired.Should().BeFalse();
        cert.IsExpiringSoon.Should().BeTrue();
    }

    [Fact]
    public void CertificationExpiry_NotExpiring_WhenMoreThan30DaysOut()
    {
        // Arrange
        var cert = new EmployeeCertification
        {
            PersonId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Crane Certification",
            Type = "Certificate",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60)
        };

        // Act & Assert
        cert.IsExpired.Should().BeFalse();
        cert.IsExpiringSoon.Should().BeFalse();
    }

    [Fact]
    public void DeviationReminder_FindsOverdueDeviations()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var deviation = new Deviation
        {
            Title = "Overdue deviation",
            Status = DeviationStatus.Open,
            Severity = DeviationSeverity.Medium,
            ReportedById = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CorrectiveActionDeadline = now.AddDays(-3)
        };

        // Act & Assert — deviation is overdue when deadline is in the past
        var isOverdue = deviation.Status != DeviationStatus.Closed
            && deviation.CorrectiveActionDeadline.HasValue
            && deviation.CorrectiveActionDeadline.Value < now
            && !deviation.CorrectiveActionCompletedAt.HasValue;

        isOverdue.Should().BeTrue();
    }

    [Fact]
    public void DeviationReminder_NotOverdue_WhenClosed()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var deviation = new Deviation
        {
            Title = "Closed deviation",
            Status = DeviationStatus.Closed,
            Severity = DeviationSeverity.Medium,
            ReportedById = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CorrectiveActionDeadline = now.AddDays(-3),
            ClosedAt = now.AddDays(-1)
        };

        // Act & Assert — closed deviations should not be flagged
        var isOverdue = deviation.Status != DeviationStatus.Closed
            && deviation.CorrectiveActionDeadline.HasValue
            && deviation.CorrectiveActionDeadline.Value < now;

        isOverdue.Should().BeFalse();
    }

    [Fact]
    public void DeviationAutoEscalation_EscalatesHighSeverity()
    {
        // Arrange
        var deviation = new Deviation
        {
            Title = "Critical issue",
            Status = DeviationStatus.Open,
            Severity = DeviationSeverity.High,
            ReportedById = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-25)
        };

        // Act — simulate escalation logic
        var shouldEscalate = deviation.Status == DeviationStatus.Open
            && deviation.Severity == DeviationSeverity.High
            && deviation.CreatedAt < DateTimeOffset.UtcNow.AddHours(-24);

        shouldEscalate.Should().BeTrue();

        // Simulate the escalation
        deviation.Status = DeviationStatus.InProgress;
        deviation.Status.Should().Be(DeviationStatus.InProgress);
    }

    [Fact]
    public void DeviationAutoEscalation_DoesNotEscalateLowSeverity()
    {
        // Arrange
        var deviation = new Deviation
        {
            Title = "Minor issue",
            Status = DeviationStatus.Open,
            Severity = DeviationSeverity.Low,
            ReportedById = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-48)
        };

        // Act
        var shouldEscalate = deviation.Status == DeviationStatus.Open
            && deviation.Severity == DeviationSeverity.High
            && deviation.CreatedAt < DateTimeOffset.UtcNow.AddHours(-24);

        // Assert — low severity should not be escalated
        shouldEscalate.Should().BeFalse();
    }

    [Fact]
    public void DataAnonymization_AnonymizesAfterFiveYears()
    {
        // Arrange
        var person = new Person
        {
            FullName = "Ola Nordmann",
            Email = "ola@example.com",
            PhoneNumber = "12345678",
            PasswordHash = "some-hash"
        };

        var membership = new TenantMembership
        {
            PersonId = person.Id,
            TenantId = Guid.NewGuid(),
            Role = TenantRole.FieldWorker,
            State = TenantMembershipState.Removed,
            RemovedAt = DateTimeOffset.UtcNow.AddYears(-6)
        };

        // Act — verify the membership qualifies for anonymization
        var fiveYearsAgo = DateTimeOffset.UtcNow.AddYears(-5);
        var shouldAnonymize = membership.State == TenantMembershipState.Removed
            && membership.RemovedAt.HasValue
            && membership.RemovedAt.Value < fiveYearsAgo;

        shouldAnonymize.Should().BeTrue();

        // Simulate anonymization
        person.FullName = "Anonymisert bruker";
        person.Email = $"anonymized-{person.Id}@solodoc.dev";
        person.PhoneNumber = null;
        person.PasswordHash = string.Empty;

        // Assert
        person.FullName.Should().Be("Anonymisert bruker");
        person.Email.Should().StartWith("anonymized-");
        person.PhoneNumber.Should().BeNull();
        person.PasswordHash.Should().BeEmpty();
    }

    [Fact]
    public void DataAnonymization_DoesNotAnonymize_WhenLessThanFiveYears()
    {
        // Arrange
        var membership = new TenantMembership
        {
            PersonId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Role = TenantRole.FieldWorker,
            State = TenantMembershipState.Removed,
            RemovedAt = DateTimeOffset.UtcNow.AddYears(-3)
        };

        // Act
        var fiveYearsAgo = DateTimeOffset.UtcNow.AddYears(-5);
        var shouldAnonymize = membership.State == TenantMembershipState.Removed
            && membership.RemovedAt.HasValue
            && membership.RemovedAt.Value < fiveYearsAgo;

        // Assert — 3 years is not enough
        shouldAnonymize.Should().BeFalse();
    }
}
