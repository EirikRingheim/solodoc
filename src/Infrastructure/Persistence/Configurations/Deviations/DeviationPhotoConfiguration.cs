using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Deviations;

namespace Solodoc.Infrastructure.Persistence.Configurations.Deviations;

public class DeviationPhotoConfiguration : IEntityTypeConfiguration<DeviationPhoto>
{
    public void Configure(EntityTypeBuilder<DeviationPhoto> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.DeviationId);

        builder.Property(p => p.FileKey).HasMaxLength(500).IsRequired();
        builder.Property(p => p.ThumbnailKey).HasMaxLength(500);
        builder.Property(p => p.AnnotatedFileKey).HasMaxLength(500);
    }
}
