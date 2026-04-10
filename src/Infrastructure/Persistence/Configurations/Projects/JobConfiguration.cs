using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Projects;

namespace Solodoc.Infrastructure.Persistence.Configurations.Projects;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);
        builder.HasIndex(j => j.TenantId);
        builder.HasIndex(j => j.CreatedById);

        builder.Property(j => j.Description).HasMaxLength(2000).IsRequired();
        builder.Property(j => j.Address).HasMaxLength(500);
        builder.Property(j => j.Notes).HasMaxLength(2000);
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(j => j.Customer)
            .WithMany()
            .HasForeignKey(j => j.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(j => j.PartsItems)
            .WithOne(p => p.Job)
            .HasForeignKey(p => p.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
