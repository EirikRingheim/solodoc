using FluentAssertions;
using Solodoc.Domain.Entities.Export;

namespace Solodoc.UnitTests.Services;

public class ExportServiceTests
{
    [Fact]
    public void ExportJob_DefaultValues_AreCorrect()
    {
        // Act
        var job = new ExportJob();

        // Assert
        job.Id.Should().NotBeEmpty();
        job.Status.Should().Be("Pending");
        job.OutputMode.Should().Be("CombinedPdf");
        job.PhotoOption.Should().Be("compressed");
        job.Type.Should().BeEmpty();
        job.TargetEntityId.Should().BeNull();
        job.SelectionJson.Should().BeNull();
        job.ResultFileKey.Should().BeNull();
        job.ResultFileName.Should().BeNull();
        job.ResultFileSizeBytes.Should().BeNull();
        job.ErrorMessage.Should().BeNull();
        job.CompletedAt.Should().BeNull();
        job.ProgressPercent.Should().BeNull();
        job.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void ExportJob_ExpiresAt_CanBeSetTo7DaysFromNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        var job = new ExportJob
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        var after = DateTimeOffset.UtcNow.AddDays(7);

        // Assert
        job.ExpiresAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        job.ExpiresAt.Should().BeOnOrBefore(after.AddSeconds(1));
    }

    [Fact]
    public void ExportJob_StatusTransition_PendingToProcessing()
    {
        // Arrange
        var job = new ExportJob { Status = "Pending" };

        // Act
        job.Status = "Processing";
        job.ProgressPercent = 0;

        // Assert
        job.Status.Should().Be("Processing");
        job.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public void ExportJob_StatusTransition_ProcessingToCompleted()
    {
        // Arrange
        var job = new ExportJob { Status = "Processing", ProgressPercent = 50 };

        // Act
        job.Status = "Completed";
        job.ProgressPercent = 100;
        job.CompletedAt = DateTimeOffset.UtcNow;
        job.ResultFileKey = "tenant/exports/123/file.pdf";
        job.ResultFileName = "file.pdf";
        job.ResultFileSizeBytes = 1024;

        // Assert
        job.Status.Should().Be("Completed");
        job.ProgressPercent.Should().Be(100);
        job.CompletedAt.Should().NotBeNull();
        job.ResultFileKey.Should().NotBeNullOrEmpty();
        job.ResultFileName.Should().Be("file.pdf");
        job.ResultFileSizeBytes.Should().Be(1024);
    }

    [Fact]
    public void ExportJob_StatusTransition_ProcessingToFailed()
    {
        // Arrange
        var job = new ExportJob { Status = "Processing", ProgressPercent = 25 };

        // Act
        job.Status = "Failed";
        job.ErrorMessage = "Something went wrong";

        // Assert
        job.Status.Should().Be("Failed");
        job.ErrorMessage.Should().Be("Something went wrong");
        job.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void ExportJob_ProjectExport_HasCorrectTypeAndTarget()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var requestedById = Guid.NewGuid();

        // Act
        var job = new ExportJob
        {
            TenantId = tenantId,
            Type = "project",
            TargetEntityId = projectId,
            OutputMode = "StructuredZip",
            PhotoOption = "compressed",
            RequestedById = requestedById,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        // Assert
        job.Type.Should().Be("project");
        job.TargetEntityId.Should().Be(projectId);
        job.TenantId.Should().Be(tenantId);
        job.RequestedById.Should().Be(requestedById);
        job.OutputMode.Should().Be("StructuredZip");
    }

    [Fact]
    public void ExportJob_EmployeeExport_HasCorrectType()
    {
        // Arrange
        var personId = Guid.NewGuid();

        // Act
        var job = new ExportJob
        {
            Type = "employee",
            TargetEntityId = personId,
            OutputMode = "CombinedPdf"
        };

        // Assert
        job.Type.Should().Be("employee");
        job.TargetEntityId.Should().Be(personId);
    }

    [Fact]
    public void ExportJob_CustomExport_StoresSelectionJson()
    {
        // Arrange
        var selectionJson = "[{\"type\":\"deviation\",\"id\":\"" + Guid.NewGuid() + "\"}]";

        // Act
        var job = new ExportJob
        {
            Type = "custom",
            SelectionJson = selectionJson,
            OutputMode = "StructuredZip"
        };

        // Assert
        job.Type.Should().Be("custom");
        job.SelectionJson.Should().NotBeNullOrEmpty();
        job.TargetEntityId.Should().BeNull();
    }

    [Fact]
    public void ExportJob_InheritsBaseEntityProperties()
    {
        // Act
        var job = new ExportJob();

        // Assert
        job.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        job.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        job.IsDeleted.Should().BeFalse();
        job.DeletedAt.Should().BeNull();
        job.DeletedBy.Should().BeNull();
    }
}
