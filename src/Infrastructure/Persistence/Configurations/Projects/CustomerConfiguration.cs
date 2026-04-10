using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Projects;

namespace Solodoc.Infrastructure.Persistence.Configurations.Projects;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.TenantId);

        builder.Property(c => c.Name).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.OrgNumber).HasMaxLength(20);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.PostalCode).HasMaxLength(10);
        builder.Property(c => c.City).HasMaxLength(200);
        builder.Property(c => c.ContactPersonName).HasMaxLength(300);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(300);
        builder.Property(c => c.Notes).HasMaxLength(2000);
    }
}
