using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Procedures;

namespace Solodoc.Infrastructure.Persistence.Configurations.Procedures;

public class ProcedureTemplateConfiguration : IEntityTypeConfiguration<ProcedureTemplate>
{
    public void Configure(EntityTypeBuilder<ProcedureTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TenantId);

        builder.Property(t => t.Name).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);
    }
}
