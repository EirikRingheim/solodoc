using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Chemicals;

namespace Solodoc.Infrastructure.Persistence.Configurations.Chemicals;

public class ChemicalConfiguration : IEntityTypeConfiguration<Chemical>
{
    public void Configure(EntityTypeBuilder<Chemical> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.TenantId);

        builder.Property(c => c.Name).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Manufacturer).HasMaxLength(300);
        builder.Property(c => c.ProductNumber).HasMaxLength(100);

        builder.HasMany(c => c.SdsDocuments)
            .WithOne(s => s.Chemical)
            .HasForeignKey(s => s.ChemicalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.GhsPictograms)
            .WithOne(g => g.Chemical)
            .HasForeignKey(g => g.ChemicalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.PpeRequirements)
            .WithOne(p => p.Chemical)
            .HasForeignKey(p => p.ChemicalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
