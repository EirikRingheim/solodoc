using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Employees;

namespace Solodoc.Infrastructure.Persistence.Configurations.Employees;

public class EmployeeCertificationConfiguration : IEntityTypeConfiguration<EmployeeCertification>
{
    public void Configure(EntityTypeBuilder<EmployeeCertification> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.PersonId, e.ExpiryDate });
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Type).HasMaxLength(100);
        builder.Property(e => e.IssuedBy).HasMaxLength(200);
        builder.Property(e => e.FileKey).HasMaxLength(500);
        builder.Property(e => e.ThumbnailKey).HasMaxLength(500);
        builder.Property(e => e.OcrStatus).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.Ignore(e => e.IsExpired);
        builder.Ignore(e => e.IsExpiringSoon);
    }
}
