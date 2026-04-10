using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hms;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hms;

public class SjaHazardConfiguration : IEntityTypeConfiguration<SjaHazard>
{
    public void Configure(EntityTypeBuilder<SjaHazard> builder)
    {
        builder.HasKey(h => h.Id);
        builder.HasIndex(h => h.SjaFormId);

        builder.Property(h => h.Description).HasMaxLength(2000).IsRequired();
        builder.Property(h => h.Mitigation).HasMaxLength(2000);
    }
}
