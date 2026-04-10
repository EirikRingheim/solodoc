using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Chemicals;

namespace Solodoc.Infrastructure.Persistence.Configurations.Chemicals;

public class ChemicalSdsConfiguration : IEntityTypeConfiguration<ChemicalSds>
{
    public void Configure(EntityTypeBuilder<ChemicalSds> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.ChemicalId);

        builder.Property(s => s.FileKey).HasMaxLength(500).IsRequired();
        builder.Property(s => s.OriginalFileName).HasMaxLength(500);
        builder.Property(s => s.Language).HasMaxLength(10);
    }
}
