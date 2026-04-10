using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Procedures;

namespace Solodoc.Infrastructure.Persistence.Configurations.Procedures;

public class ProcedureBlockConfiguration : IEntityTypeConfiguration<ProcedureBlock>
{
    public void Configure(EntityTypeBuilder<ProcedureBlock> builder)
    {
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.ProcedureTemplateId, b.SortOrder });

        builder.Property(b => b.Type).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Content).HasMaxLength(8000).IsRequired();

        builder.HasOne(b => b.ProcedureTemplate)
            .WithMany()
            .HasForeignKey(b => b.ProcedureTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
