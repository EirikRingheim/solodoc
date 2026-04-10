using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Auth;

namespace Solodoc.Infrastructure.Persistence.Configurations.Auth;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.HasIndex(i => new { i.TenantId, i.Email });

        builder.Property(i => i.Email).HasMaxLength(256).IsRequired();
        builder.Property(i => i.InvitedByName).HasMaxLength(200).IsRequired();
    }
}
