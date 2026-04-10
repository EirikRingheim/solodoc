using FluentAssertions;
using Solodoc.Domain.Entities.Chemicals;

namespace Solodoc.UnitTests.Domain;

public class ChemicalTests
{
    [Fact]
    public void Chemical_DefaultIsActive_IsTrue()
    {
        var chemical = new Chemical();

        chemical.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Chemical_Deactivated_IsActiveFalse()
    {
        var chemical = new Chemical { IsActive = false };

        chemical.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Chemical_DefaultCollections_AreEmpty()
    {
        var chemical = new Chemical();

        chemical.SdsDocuments.Should().BeEmpty();
        chemical.GhsPictograms.Should().BeEmpty();
        chemical.PpeRequirements.Should().BeEmpty();
    }

    [Fact]
    public void ChemicalGhsPictogram_HasCode()
    {
        var pictogram = new ChemicalGhsPictogram
        {
            ChemicalId = Guid.NewGuid(),
            PictogramCode = "GHS02",
            Description = "Flammable"
        };

        pictogram.PictogramCode.Should().Be("GHS02");
        pictogram.PictogramCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ChemicalGhsPictogram_DefaultCode_IsEmpty()
    {
        var pictogram = new ChemicalGhsPictogram();

        pictogram.PictogramCode.Should().Be(string.Empty);
    }
}
