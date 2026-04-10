using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Deviations;

namespace Solodoc.Infrastructure.Persistence.Configurations.Deviations;

public class RelatedDeviationConfiguration : IEntityTypeConfiguration<RelatedDeviation>
{
    public void Configure(EntityTypeBuilder<RelatedDeviation> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.DeviationId, r.RelatedDeviationId }).IsUnique();

        builder.HasOne(r => r.Deviation)
            .WithMany(d => d.RelatedDeviations)
            .HasForeignKey(r => r.DeviationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Related)
            .WithMany()
            .HasForeignKey(r => r.RelatedDeviationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
