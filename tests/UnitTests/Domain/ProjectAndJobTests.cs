using FluentAssertions;
using Solodoc.Domain.Entities.Projects;
using Solodoc.Domain.Enums;

namespace Solodoc.UnitTests.Domain;

public class ProjectAndJobTests
{
    [Fact]
    public void Project_Create_SetsStatusToPlanlagt()
    {
        // Arrange & Act
        var project = new Project();

        // Assert
        project.Status.Should().Be(ProjectStatus.Planlagt);
    }

    [Fact]
    public void Project_Complete_SetsStatusToCompleted()
    {
        // Arrange
        var project = new Project();

        // Act
        project.Status = ProjectStatus.Completed;

        // Assert
        project.Status.Should().Be(ProjectStatus.Completed);
    }

    [Fact]
    public void Job_Create_SetsStatusToActive()
    {
        // Arrange & Act
        var job = new Job();

        // Assert
        job.Status.Should().Be(JobStatus.Active);
    }

    [Fact]
    public void Job_AddPartsItem_HasCorrectDefaults()
    {
        // Arrange
        var job = new Job();

        // Act
        var partsItem = new JobPartsItem
        {
            JobId = job.Id,
            Description = "Bremseskive",
            AddedById = Guid.NewGuid()
        };
        job.PartsItems.Add(partsItem);

        // Assert
        partsItem.Status.Should().Be(PartsItemStatus.Trengs);
        job.PartsItems.Should().ContainSingle()
            .Which.Should().BeSameAs(partsItem);
    }

    [Fact]
    public void Customer_Bedrift_HasCorrectType()
    {
        // Arrange & Act
        var customer = new Customer
        {
            Name = "Fjellbygg AS",
            Type = CustomerType.Bedrift,
            OrgNumber = "123456789"
        };

        // Assert
        customer.Type.Should().Be(CustomerType.Bedrift);
    }
}
