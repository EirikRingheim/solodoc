using FluentAssertions;
using Solodoc.Domain.Entities.Deviations;
using Solodoc.Domain.Enums;

namespace Solodoc.UnitTests.Domain;

public class DeviationTests
{
    [Fact]
    public void Deviation_Create_SetsStatusToOpen()
    {
        var deviation = new Deviation { Title = "Test" };

        deviation.Status.Should().Be(DeviationStatus.Open);
    }

    [Fact]
    public void Deviation_Assign_ChangesStatusToInProgress()
    {
        var deviation = new Deviation { Title = "Test" };

        deviation.Status = DeviationStatus.InProgress;
        deviation.AssignedToId = Guid.NewGuid();

        deviation.Status.Should().Be(DeviationStatus.InProgress);
        deviation.AssignedToId.Should().NotBeNull();
    }

    [Fact]
    public void Deviation_Close_SetsStatusToClosed()
    {
        var deviation = new Deviation { Title = "Test", Status = DeviationStatus.InProgress };

        deviation.Status = DeviationStatus.Closed;
        deviation.ClosedAt = DateTimeOffset.UtcNow;
        deviation.ClosedById = Guid.NewGuid();

        deviation.Status.Should().Be(DeviationStatus.Closed);
        deviation.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deviation_Reopen_SetsStatusToInProgress()
    {
        var deviation = new Deviation
        {
            Title = "Test",
            Status = DeviationStatus.Closed,
            ClosedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        deviation.Status = DeviationStatus.InProgress;
        deviation.ClosedAt = null;
        deviation.ClosedById = null;

        deviation.Status.Should().Be(DeviationStatus.InProgress);
        deviation.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void Deviation_Personskade_HasInjuryFields()
    {
        var deviation = new Deviation
        {
            Title = "Fall fra stillas",
            Type = DeviationType.Personskade,
            InjuryDescription = "Brudd i venstre underarm",
            BodyPart = "Venstre underarm",
            FirstAidGiven = true,
            HospitalVisit = true
        };

        deviation.Type.Should().Be(DeviationType.Personskade);
        deviation.InjuryDescription.Should().NotBeNullOrEmpty();
        deviation.BodyPart.Should().NotBeNullOrEmpty();
        deviation.FirstAidGiven.Should().BeTrue();
        deviation.HospitalVisit.Should().BeTrue();
    }

    [Fact]
    public void Deviation_DefaultSeverity_IsMedium()
    {
        var deviation = new Deviation();

        deviation.Severity.Should().Be(DeviationSeverity.Medium);
    }

    [Fact]
    public void Deviation_ConfidentialDefault_IsFalse()
    {
        var deviation = new Deviation();

        deviation.IsConfidential.Should().BeFalse();
    }

    [Fact]
    public void Deviation_WithCategory_HasCategoryId()
    {
        var categoryId = Guid.NewGuid();
        var deviation = new Deviation
        {
            Title = "Manglende sikring",
            CategoryId = categoryId
        };

        deviation.CategoryId.Should().Be(categoryId);
        deviation.CategoryId.Should().NotBeNull();
    }

    [Fact]
    public void Deviation_DefaultPhotos_IsEmpty()
    {
        var deviation = new Deviation();

        deviation.Photos.Should().BeEmpty();
    }

    [Fact]
    public void Deviation_DefaultComments_IsEmpty()
    {
        var deviation = new Deviation();

        deviation.Comments.Should().BeEmpty();
    }
}
