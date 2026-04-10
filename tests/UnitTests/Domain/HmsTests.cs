using FluentAssertions;
using Solodoc.Domain.Entities.Hms;

namespace Solodoc.UnitTests.Domain;

public class HmsTests
{
    [Fact]
    public void SjaHazard_RiskScore_IsProbabilityTimesConsequence()
    {
        var hazard = new SjaHazard
        {
            Description = "Fall from height",
            Probability = 3,
            Consequence = 4,
            RiskScore = 3 * 4
        };

        hazard.RiskScore.Should().Be(12);
    }

    [Fact]
    public void SjaHazard_HighRisk_WhenScoreAbove15()
    {
        var hazard = new SjaHazard
        {
            Description = "Electrical shock",
            Probability = 4,
            Consequence = 5,
            RiskScore = 4 * 5
        };

        hazard.RiskScore.Should().BeGreaterThan(15);
    }

    [Fact]
    public void SjaHazard_LowRisk_WhenScoreBelow6()
    {
        var hazard = new SjaHazard
        {
            Description = "Minor trip hazard",
            Probability = 1,
            Consequence = 2,
            RiskScore = 1 * 2
        };

        hazard.RiskScore.Should().BeLessThan(6);
    }

    [Fact]
    public void SjaForm_DefaultStatus_IsDraft()
    {
        var form = new SjaForm();

        form.Status.Should().Be("Draft");
    }

    [Fact]
    public void SjaForm_HasEmptyParticipants_ByDefault()
    {
        var form = new SjaForm();

        form.Participants.Should().BeEmpty();
    }

    [Fact]
    public void SjaForm_HasEmptyHazards_ByDefault()
    {
        var form = new SjaForm();

        form.Hazards.Should().BeEmpty();
    }

    [Fact]
    public void SjaParticipant_DefaultHasSigned_IsFalse()
    {
        var participant = new SjaParticipant
        {
            SjaFormId = Guid.NewGuid(),
            PersonId = Guid.NewGuid()
        };

        participant.SignedAt.Should().BeNull();
        participant.SignatureFileKey.Should().BeNull();
    }

    [Fact]
    public void SjaParticipant_AfterSigning_HasSignedAtAndSignature()
    {
        var participant = new SjaParticipant
        {
            SjaFormId = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            SignedAt = DateTimeOffset.UtcNow,
            SignatureFileKey = "signatures/abc123.png"
        };

        participant.SignedAt.Should().NotBeNull();
        participant.SignatureFileKey.Should().NotBeNullOrEmpty();
    }
}
