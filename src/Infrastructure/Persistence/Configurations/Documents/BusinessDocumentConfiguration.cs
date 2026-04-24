using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Documents;

namespace Solodoc.Infrastructure.Persistence.Configurations.Documents;

public class BusinessDocumentConfiguration : IEntityTypeConfiguration<BusinessDocument>
{
    public void Configure(EntityTypeBuilder<BusinessDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => new { d.TenantId, d.DocumentType });

        builder.Property(d => d.Title).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Status).HasMaxLength(50);
    }
}

public class WasteDisposalEntryConfiguration : IEntityTypeConfiguration<WasteDisposalEntry>
{
    public void Configure(EntityTypeBuilder<WasteDisposalEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.BusinessDocumentId);

        builder.HasOne(e => e.Document)
            .WithMany(d => d.WasteEntries)
            .HasForeignKey(e => e.BusinessDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
