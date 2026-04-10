using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Translations;

namespace Solodoc.Infrastructure.Persistence.Configurations.Translations;

public class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    public void Configure(EntityTypeBuilder<Translation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.EntityType).HasMaxLength(200).IsRequired();
        builder.Property(t => t.FieldName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(t => t.SourceLanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(t => t.TranslatedText).IsRequired();
        builder.Property(t => t.SourceText).IsRequired();

        // Index for cache lookups
        builder.HasIndex(t => new { t.SourceText, t.SourceLanguageCode, t.LanguageCode });

        // Index for entity-based lookups
        builder.HasIndex(t => new { t.EntityType, t.EntityId, t.FieldName, t.LanguageCode });
    }
}
