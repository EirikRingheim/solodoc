using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Help;

namespace Solodoc.Infrastructure.Persistence.Configurations.Help;

public class HelpContentConfiguration : IEntityTypeConfiguration<HelpContent>
{
    public void Configure(EntityTypeBuilder<HelpContent> builder)
    {
        builder.HasKey(h => h.Id);
        builder.HasIndex(h => new { h.PageIdentifier, h.Language });

        builder.Property(h => h.PageIdentifier).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Title).HasMaxLength(300).IsRequired();
        builder.Property(h => h.Body).HasMaxLength(8000).IsRequired();
        builder.Property(h => h.RoleScope).HasMaxLength(100);
        builder.Property(h => h.Language).HasMaxLength(10).IsRequired();
    }
}
