using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Billing;

namespace Solodoc.Infrastructure.Persistence.Configurations.Billing;

public class ClientErrorConfiguration : IEntityTypeConfiguration<ClientError>
{
    public void Configure(EntityTypeBuilder<ClientError> builder)
    {
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.IsResolved, e.CreatedAt });

        builder.Property(e => e.UserEmail).HasMaxLength(254);
        builder.Property(e => e.Message).HasMaxLength(2000);
        builder.Property(e => e.StackTrace).HasMaxLength(8000);
        builder.Property(e => e.Page).HasMaxLength(500);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.AdditionalInfo).HasMaxLength(2000);
    }
}
