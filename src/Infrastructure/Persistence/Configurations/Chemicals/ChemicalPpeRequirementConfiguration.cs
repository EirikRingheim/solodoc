using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Chemicals;

namespace Solodoc.Infrastructure.Persistence.Configurations.Chemicals;

public class ChemicalPpeRequirementConfiguration : IEntityTypeConfiguration<ChemicalPpeRequirement>
{
    public void Configure(EntityTypeBuilder<ChemicalPpeRequirement> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.ChemicalId);

        builder.Property(p => p.Requirement).HasMaxLength(300).IsRequired();
        builder.Property(p => p.IconCode).HasMaxLength(50);
    }
}
