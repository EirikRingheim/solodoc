using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Chemicals;

namespace Solodoc.Infrastructure.Persistence.Configurations.Chemicals;

public class ChemicalGhsPictogramConfiguration : IEntityTypeConfiguration<ChemicalGhsPictogram>
{
    public void Configure(EntityTypeBuilder<ChemicalGhsPictogram> builder)
    {
        builder.HasKey(g => g.Id);
        builder.HasIndex(g => g.ChemicalId);

        builder.Property(g => g.PictogramCode).HasMaxLength(10).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(200);
    }
}
