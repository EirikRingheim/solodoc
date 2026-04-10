using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Employees;

namespace Solodoc.Infrastructure.Persistence.Configurations.Employees;

public class InternalTrainingConfiguration : IEntityTypeConfiguration<InternalTraining>
{
    public void Configure(EntityTypeBuilder<InternalTraining> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.TraineeId });
        builder.HasIndex(e => e.TrainerId);

        builder.Property(e => e.Topic).HasMaxLength(300);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.SignatureFileKey).HasMaxLength(500);
        builder.Property(e => e.DurationHours).HasPrecision(5, 2);
    }
}
