using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Contacts;

namespace Solodoc.Infrastructure.Persistence.Configurations.Contacts;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.TenantId);

        builder.Property(c => c.Name).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.OrgNumber).HasMaxLength(20);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.PostalCode).HasMaxLength(10);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.Title).HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.Property(c => c.Notes).HasMaxLength(4000);

        builder.HasMany(c => c.ProjectLinks)
            .WithOne(l => l.Contact)
            .HasForeignKey(l => l.ContactId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
