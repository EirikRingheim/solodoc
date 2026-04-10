using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Projects;

namespace Solodoc.Infrastructure.Persistence.Configurations.Projects;

public class JobPartsItemConfiguration : IEntityTypeConfiguration<JobPartsItem>
{
    public void Configure(EntityTypeBuilder<JobPartsItem> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.JobId);

        builder.Property(p => p.Description).HasMaxLength(2000).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(p => p.Job)
            .WithMany(j => j.PartsItems)
            .HasForeignKey(p => p.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
