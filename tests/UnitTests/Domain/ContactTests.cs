using FluentAssertions;
using Solodoc.Domain.Entities.Contacts;
using Solodoc.Domain.Enums;

namespace Solodoc.UnitTests.Domain;

public class ContactTests
{
    [Fact]
    public void Contact_DefaultType_IsKunde()
    {
        var contact = new Contact();

        // Default enum value is the first member (Kunde = 0)
        contact.Type.Should().Be(ContactType.Kunde);
    }

    [Fact]
    public void Contact_WithOrgNumber_CanBeKunde()
    {
        var contact = new Contact
        {
            Name = "Fjellbygg AS",
            Type = ContactType.Kunde,
            OrgNumber = "912345678"
        };

        contact.Type.Should().Be(ContactType.Kunde);
        contact.OrgNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Contact_AsLeverandor_HasCorrectType()
    {
        var contact = new Contact
        {
            Name = "Byggmax",
            Type = ContactType.Leverandor
        };

        contact.Type.Should().Be(ContactType.Leverandor);
    }

    [Fact]
    public void Contact_DefaultProjectLinks_IsEmpty()
    {
        var contact = new Contact();

        contact.ProjectLinks.Should().BeEmpty();
    }
}
