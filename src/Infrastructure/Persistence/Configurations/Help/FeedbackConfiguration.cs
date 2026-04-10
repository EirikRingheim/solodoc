using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Help;

namespace Solodoc.Infrastructure.Persistence.Configurations.Help;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.HasKey(f => f.Id);
        builder.HasIndex(f => f.PersonId);

        builder.Property(f => f.Page).HasMaxLength(200);
        builder.Property(f => f.Type).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Message).HasMaxLength(4000).IsRequired();
    }
}
