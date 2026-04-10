using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Projects;

namespace Solodoc.Infrastructure.Persistence.Configurations.Projects;

public class ProjectMembershipConfiguration : IEntityTypeConfiguration<ProjectMembership>
{
    public void Configure(EntityTypeBuilder<ProjectMembership> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.ProjectId, m.PersonId }).IsUnique();

        builder.Property(m => m.Role).HasMaxLength(100);

        builder.HasOne(m => m.Project)
            .WithMany(p => p.Memberships)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
