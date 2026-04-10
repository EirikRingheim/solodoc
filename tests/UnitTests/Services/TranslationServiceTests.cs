using FluentAssertions;
using Solodoc.Domain.Entities.Translations;
using Solodoc.Infrastructure.Services;

namespace Solodoc.UnitTests.Services;

public class TranslationServiceTests
{
    [Fact]
    public void GetGlossaryTerm_KnownTerm_ReturnsTranslation()
    {
        var result = TranslationService.GetGlossaryTerm("avvik", "EN");

        result.Should().Be("deviation");
    }

    [Fact]
    public void GetGlossaryTerm_CaseInsensitive_ReturnsTranslation()
    {
        var result = TranslationService.GetGlossaryTerm("Avvik", "en");

        result.Should().Be("deviation");
    }

    [Fact]
    public void GetGlossaryTerm_UnknownTerm_ReturnsNull()
    {
        var result = TranslationService.GetGlossaryTerm("ukjent", "EN");

        result.Should().BeNull();
    }

    [Fact]
    public void GetGlossaryTerm_UnknownLanguage_ReturnsNull()
    {
        var result = TranslationService.GetGlossaryTerm("avvik", "FR");

        result.Should().BeNull();
    }

    [Fact]
    public void GetGlossaryTerm_HmsToEnglish_ReturnsHse()
    {
        var result = TranslationService.GetGlossaryTerm("HMS", "EN");

        result.Should().Be("HSE");
    }

    [Fact]
    public void GetGlossaryTerm_PolishTerms_ReturnsCorrectTranslation()
    {
        var result = TranslationService.GetGlossaryTerm("avvik", "PL");

        result.Should().Be("odchylenie");
    }

    [Fact]
    public void GetGlossaryTerm_SpanishTerms_ReturnsCorrectTranslation()
    {
        var result = TranslationService.GetGlossaryTerm("sjekkliste", "ES");

        result.Should().Be("lista de verificacion");
    }

    [Fact]
    public void Translation_Entity_HasCorrectDefaults()
    {
        var translation = new Translation();

        translation.Id.Should().NotBeEmpty();
        translation.SourceLanguageCode.Should().Be("nb");
        translation.EntityType.Should().BeEmpty();
        translation.TranslatedText.Should().BeEmpty();
    }

    [Fact]
    public void Translation_Entity_CanSetProperties()
    {
        var entityId = Guid.NewGuid();
        var translation = new Translation
        {
            EntityType = "Project",
            EntityId = entityId,
            FieldName = "Name",
            LanguageCode = "EN",
            TranslatedText = "Test Project",
            SourceLanguageCode = "NB",
            SourceText = "Testprosjekt"
        };

        translation.EntityType.Should().Be("Project");
        translation.EntityId.Should().Be(entityId);
        translation.FieldName.Should().Be("Name");
        translation.LanguageCode.Should().Be("EN");
        translation.TranslatedText.Should().Be("Test Project");
        translation.SourceLanguageCode.Should().Be("NB");
        translation.SourceText.Should().Be("Testprosjekt");
    }
}
