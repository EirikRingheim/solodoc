using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hours;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hours;

public class AllowanceGroupConfiguration : IEntityTypeConfiguration<AllowanceGroup>
{
    public void Configure(EntityTypeBuilder<AllowanceGroup> builder)
    {
        builder.HasKey(g => g.Id);
        builder.HasIndex(g => g.TenantId);

        builder.Property(g => g.Name).HasMaxLength(200).IsRequired();

        builder.HasMany(g => g.Members)
            .WithOne(m => m.AllowanceGroup)
            .HasForeignKey(m => m.AllowanceGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Rules)
            .WithOne(r => r.AllowanceGroup)
            .HasForeignKey(r => r.AllowanceGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
