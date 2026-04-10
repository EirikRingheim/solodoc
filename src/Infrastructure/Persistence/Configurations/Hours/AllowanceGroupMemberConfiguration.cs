using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hours;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hours;

public class AllowanceGroupMemberConfiguration : IEntityTypeConfiguration<AllowanceGroupMember>
{
    public void Configure(EntityTypeBuilder<AllowanceGroupMember> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.AllowanceGroupId, m.PersonId }).IsUnique();
    }
}
