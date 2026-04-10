using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hms;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hms;

public class SjaFormConfiguration : IEntityTypeConfiguration<SjaForm>
{
    public void Configure(EntityTypeBuilder<SjaForm> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.ProjectId);
        builder.HasIndex(s => s.CreatedById);

        builder.Property(s => s.Title).HasMaxLength(300).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.Status).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Location).HasMaxLength(500);

        builder.HasMany(s => s.Participants)
            .WithOne(p => p.SjaForm)
            .HasForeignKey(p => p.SjaFormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Hazards)
            .WithOne(h => h.SjaForm)
            .HasForeignKey(h => h.SjaFormId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
