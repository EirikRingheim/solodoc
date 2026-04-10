using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Contacts;

namespace Solodoc.Infrastructure.Persistence.Configurations.Contacts;

public class ContactProjectLinkConfiguration : IEntityTypeConfiguration<ContactProjectLink>
{
    public void Configure(EntityTypeBuilder<ContactProjectLink> builder)
    {
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.ContactId);
        builder.HasIndex(l => l.ProjectId);

        builder.Property(l => l.Role).HasMaxLength(200);
    }
}
