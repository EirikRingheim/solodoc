using FluentAssertions;
using Solodoc.Domain.Entities.Checklists;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Services;

namespace Solodoc.UnitTests.Domain;

public class ChecklistTests
{
    [Fact]
    public void Template_NewVersion_IncrementsVersionNumber()
    {
        var template = new ChecklistTemplate { Name = "Brannsjekk", CurrentVersion = 1 };

        template.CurrentVersion++;
        var newVersion = new ChecklistTemplateVersion
        {
            ChecklistTemplateId = template.Id,
            VersionNumber = template.CurrentVersion,
            PublishedAt = DateTimeOffset.UtcNow,
            PublishedById = Guid.NewGuid()
        };

        newVersion.VersionNumber.Should().Be(2);
        template.CurrentVersion.Should().Be(2);
    }

    [Fact]
    public void Template_Publish_SetsIsPublished()
    {
        var template = new ChecklistTemplate { Name = "Stillassjekk", IsPublished = false };

        template.IsPublished = true;
        template.CurrentVersion = 1;

        template.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Instance_DefaultStatus_IsDraft()
    {
        var instance = new ChecklistInstance();
        instance.Status.Should().Be(ChecklistInstanceStatus.Draft);
    }

    [Fact]
    public void Instance_FrozenToTemplateVersion_ReferencesVersionNotTemplate()
    {
        var versionId = Guid.NewGuid();
        var instance = new ChecklistInstance
        {
            TemplateVersionId = versionId,
            StartedById = Guid.NewGuid()
        };

        instance.TemplateVersionId.Should().Be(versionId);
    }

    [Fact]
    public void ChecklistInstance_DefaultStatus_IsDraftExplicit()
    {
        var instance = new ChecklistInstance
        {
            TemplateVersionId = Guid.NewGuid(),
            StartedById = Guid.NewGuid()
        };

        instance.Status.Should().Be(ChecklistInstanceStatus.Draft);
        instance.SubmittedAt.Should().BeNull();
        instance.ApprovedAt.Should().BeNull();
    }

    [Fact]
    public void ChecklistTemplateItem_IsRequired_DefaultIsFalse()
    {
        var item = new ChecklistTemplateItem();

        item.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void ChecklistTemplateItem_WithRequired_IsTrue()
    {
        var item = new ChecklistTemplateItem
        {
            Label = "Brannslukker kontrollert",
            IsRequired = true,
            Type = ChecklistItemType.Check
        };

        item.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void Template_NewFields_HaveCorrectDefaults()
    {
        var template = new ChecklistTemplate();

        template.RequireSignature.Should().BeTrue();
        template.SignatureCount.Should().Be(1);
        template.IsBaseTemplate.Should().BeFalse();
        template.IsLocked.Should().BeFalse();
        template.Category.Should().BeNull();
        template.DocumentNumber.Should().BeNull();
        template.SignatureRoles.Should().BeNull();
        template.BaseTemplateId.Should().BeNull();
    }

    [Fact]
    public void TemplateItem_NewFields_HaveCorrectDefaults()
    {
        var item = new ChecklistTemplateItem();

        item.RequireCommentOnIrrelevant.Should().BeFalse();
        item.AllowPhoto.Should().BeFalse();
        item.AllowComment.Should().BeFalse();
        item.UnitLabel.Should().BeNull();
        item.Source.Should().Be("tenant");
    }

    [Fact]
    public void InstanceItem_NewFields_HaveCorrectDefaults()
    {
        var item = new ChecklistInstanceItem();

        item.IsIrrelevant.Should().BeFalse();
        item.IrrelevantComment.Should().BeNull();
        item.Comment.Should().BeNull();
        item.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Instance_ReopenedFields_DefaultToNull()
    {
        var instance = new ChecklistInstance();

        instance.ReopenedAt.Should().BeNull();
        instance.ReopenedById.Should().BeNull();
        instance.ReopenedReason.Should().BeNull();
        instance.OriginalSnapshotJson.Should().BeNull();
        instance.LocationIdentifier.Should().BeNull();
    }

    [Fact]
    public void AutoTagger_ExtractTags_RemovesStopWords()
    {
        var tags = AutoTagger.ExtractTags("Sjekkliste for brannvern", new[] { "Kontroller brannslukker", "Sjekk nødutgang" });

        tags.Should().Contain("sjekkliste");
        tags.Should().Contain("brannvern");
        tags.Should().Contain("kontroller");
        tags.Should().Contain("brannslukker");
        tags.Should().Contain("sjekk");
        tags.Should().Contain("nødutgang");
        tags.Should().NotContain("for");
    }

    [Fact]
    public void AutoTagger_ExtractTags_RemovesShortWords()
    {
        var tags = AutoTagger.ExtractTags("A og B", Enumerable.Empty<string>());

        tags.Should().BeEmpty();
    }

    [Fact]
    public void AutoTagger_ExtractTags_LimitsToTenTags()
    {
        var labels = Enumerable.Range(1, 20).Select(i => $"Unikt ord nummer{i}");
        var tags = AutoTagger.ExtractTags("Testmal", labels);

        tags.Split(',').Should().HaveCountLessThanOrEqualTo(10);
    }

    [Fact]
    public void ChecklistInstanceStatus_HasAllExpectedValues()
    {
        Enum.GetValues<ChecklistInstanceStatus>().Should().HaveCount(4);
        Enum.IsDefined(ChecklistInstanceStatus.Draft).Should().BeTrue();
        Enum.IsDefined(ChecklistInstanceStatus.Submitted).Should().BeTrue();
        Enum.IsDefined(ChecklistInstanceStatus.Approved).Should().BeTrue();
        Enum.IsDefined(ChecklistInstanceStatus.Reopened).Should().BeTrue();
    }
}
