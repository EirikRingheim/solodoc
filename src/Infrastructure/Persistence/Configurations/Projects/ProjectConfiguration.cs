using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Projects;

namespace Solodoc.Infrastructure.Persistence.Configurations.Projects;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.QrCodeSlug).IsUnique().HasFilter("qr_code_slug IS NOT NULL");

        builder.Property(p => p.Name).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.ClientName).HasMaxLength(300);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.QrCodeSlug).HasMaxLength(100);

        builder.HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Memberships)
            .WithOne(m => m.Project)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
