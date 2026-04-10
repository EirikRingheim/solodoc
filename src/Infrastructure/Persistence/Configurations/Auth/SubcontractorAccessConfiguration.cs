using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Auth;

namespace Solodoc.Infrastructure.Persistence.Configurations.Auth;

public class SubcontractorAccessConfiguration : IEntityTypeConfiguration<SubcontractorAccess>
{
    public void Configure(EntityTypeBuilder<SubcontractorAccess> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasIndex(s => new { s.PersonId, s.ProjectId }).IsUnique();
    }
}
