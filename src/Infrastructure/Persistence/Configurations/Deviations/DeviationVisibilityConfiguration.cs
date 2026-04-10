using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Deviations;

namespace Solodoc.Infrastructure.Persistence.Configurations.Deviations;

public class DeviationVisibilityConfiguration : IEntityTypeConfiguration<DeviationVisibility>
{
    public void Configure(EntityTypeBuilder<DeviationVisibility> builder)
    {
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => new { v.DeviationId, v.PersonId }).IsUnique();
        builder.HasIndex(v => v.PersonId);
    }
}
