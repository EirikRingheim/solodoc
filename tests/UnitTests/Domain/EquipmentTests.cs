using FluentAssertions;
using Solodoc.Domain.Entities.Equipment;

namespace Solodoc.UnitTests.Domain;

public class EquipmentTests
{
    [Fact]
    public void Equipment_DefaultIsActive_IsTrue()
    {
        var equipment = new Equipment();

        equipment.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Equipment_Deactivated_IsActiveFalse()
    {
        var equipment = new Equipment { IsActive = false };

        equipment.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Equipment_DefaultCollections_AreEmpty()
    {
        var equipment = new Equipment();

        equipment.MaintenanceRecords.Should().BeEmpty();
        equipment.Inspections.Should().BeEmpty();
        equipment.ProjectAssignments.Should().BeEmpty();
    }

    [Fact]
    public void EquipmentMaintenance_HasDate()
    {
        var date = new DateOnly(2026, 3, 15);
        var maintenance = new EquipmentMaintenance
        {
            EquipmentId = Guid.NewGuid(),
            Description = "Oil change",
            Date = date
        };

        maintenance.Date.Should().Be(date);
    }

    [Fact]
    public void EquipmentMaintenance_HasCost()
    {
        var maintenance = new EquipmentMaintenance
        {
            EquipmentId = Guid.NewGuid(),
            Description = "Engine repair",
            Date = new DateOnly(2026, 3, 15),
            Cost = 4500.00m
        };

        maintenance.Cost.Should().Be(4500.00m);
    }
}
