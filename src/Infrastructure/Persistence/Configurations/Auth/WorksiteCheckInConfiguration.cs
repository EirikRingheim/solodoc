using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Auth;

namespace Solodoc.Infrastructure.Persistence.Configurations.Auth;

public class WorksiteCheckInConfiguration : IEntityTypeConfiguration<WorksiteCheckIn>
{
    public void Configure(EntityTypeBuilder<WorksiteCheckIn> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.ProjectId, c.CheckedInAt });
        builder.HasIndex(c => new { c.PersonId, c.CheckedInAt });

        builder.HasOne(c => c.Person)
            .WithMany()
            .HasForeignKey(c => c.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
