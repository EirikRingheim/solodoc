using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Deviations;

namespace Solodoc.Infrastructure.Persistence.Configurations.Deviations;

public class DeviationCommentConfiguration : IEntityTypeConfiguration<DeviationComment>
{
    public void Configure(EntityTypeBuilder<DeviationComment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.DeviationId);

        builder.Property(c => c.Text).HasMaxLength(4000).IsRequired();
    }
}
