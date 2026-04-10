using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Deviations;

namespace Solodoc.Infrastructure.Persistence.Configurations.Deviations;

public class DeviationConfiguration : IEntityTypeConfiguration<Deviation>
{
    public void Configure(EntityTypeBuilder<Deviation> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => new { d.TenantId, d.Status });

        builder.Property(d => d.Title).HasMaxLength(300).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(4000);
        builder.Property(d => d.CorrectiveAction).HasMaxLength(4000);
        builder.Property(d => d.InjuryDescription).HasMaxLength(2000);
        builder.Property(d => d.BodyPart).HasMaxLength(300);

        builder.HasOne(d => d.Category)
            .WithMany()
            .HasForeignKey(d => d.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(d => d.Photos)
            .WithOne(p => p.Deviation)
            .HasForeignKey(p => p.DeviationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Comments)
            .WithOne(c => c.Deviation)
            .HasForeignKey(c => c.DeviationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.RelatedDeviations)
            .WithOne(r => r.Deviation)
            .HasForeignKey(r => r.DeviationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.VisibleTo)
            .WithOne(v => v.Deviation)
            .HasForeignKey(v => v.DeviationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
