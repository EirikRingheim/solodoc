using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Export;

namespace Solodoc.Infrastructure.Persistence.Configurations.Export;

public class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.RequestedById);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ExpiresAt);

        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.OutputMode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PhotoOption).HasMaxLength(50);
        builder.Property(e => e.ResultFileKey).HasMaxLength(500);
        builder.Property(e => e.ResultFileName).HasMaxLength(300);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
    }
}
