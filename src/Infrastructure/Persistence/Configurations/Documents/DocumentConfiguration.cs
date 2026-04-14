using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Documents;

namespace Solodoc.Infrastructure.Persistence.Configurations.Documents;

public class DocumentFolderConfiguration : IEntityTypeConfiguration<DocumentFolder>
{
    public void Configure(EntityTypeBuilder<DocumentFolder> builder)
    {
        builder.HasIndex(f => new { f.TenantId, f.ProjectId });
        builder.Property(f => f.Name).HasMaxLength(300);
        builder.Property(f => f.Description).HasMaxLength(1000);

        builder.HasOne(f => f.ParentFolder)
            .WithMany(f => f.SubFolders)
            .HasForeignKey(f => f.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.Documents)
            .WithOne(d => d.Folder)
            .HasForeignKey(d => d.FolderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasIndex(d => new { d.TenantId, d.FolderId });
        builder.Property(d => d.Name).HasMaxLength(500);
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.Property(d => d.FileKey).HasMaxLength(500);
        builder.Property(d => d.ContentType).HasMaxLength(100);
        builder.Property(d => d.Category).HasMaxLength(100);
    }
}
