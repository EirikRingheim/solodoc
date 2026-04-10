using FluentAssertions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Solodoc.UnitTests.Services;

public class PdfReportServiceTests
{
    static PdfReportServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void GenerateDeviationPdf_ProducesValidBytes()
    {
        // Arrange & Act
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Solodoc").Bold().FontSize(16);
                        col.Item().Text("Fjellbygg AS").FontSize(10).FontColor(Colors.Grey.Medium);
                        col.Item().Text("Avviksrapport").FontSize(10);
                    });
                    row.ConstantItem(120).AlignRight().Text(text =>
                    {
                        text.Span("Dokument: ").FontSize(8);
                        text.Span("AVV-12345678").Bold().FontSize(8);
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Manglende sikring av stillaser").Bold().FontSize(14);
                    col.Item().Text(t => { t.Span("Status: ").SemiBold(); t.Span("Apen"); });
                    col.Item().Text(t => { t.Span("Alvorlighetsgrad: ").SemiBold(); t.Span("Hoy"); });
                    col.Item().Text(t => { t.Span("Rapportert av: ").SemiBold(); t.Span("Ola Nordmann"); });
                    col.Item().PaddingTop(5).Text("Beskrivelse").SemiBold().FontSize(11);
                    col.Item().Text("Stillasene mangler sikring i tredje etasje.");
                    col.Item().PaddingTop(5).Text("Korrigerende tiltak").SemiBold().FontSize(11);
                    col.Item().Text("Monter rekkverk og fotlister umiddelbart.");
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("Generert: 01.04.2026").FontSize(7);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Side ").FontSize(7);
                        text.CurrentPageNumber().FontSize(7);
                    });
                });
            });
        }).GeneratePdf();

        // Assert
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
        // PDF files start with %PDF
        bytes[0].Should().Be(0x25); // %
        bytes[1].Should().Be(0x50); // P
        bytes[2].Should().Be(0x44); // D
        bytes[3].Should().Be(0x46); // F
    }

    [Fact]
    public void GenerateChecklistPdf_WithItemsTable_ProducesValidBytes()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Sjekkliste").Bold().FontSize(16);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Brannsikkerhet kontroll").Bold().FontSize(14);
                    col.Item().Text(t => { t.Span("Status: ").SemiBold(); t.Span("Fullfort"); });

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Punkt").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Type").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Svar").SemiBold();
                        });

                        var items = new[]
                        {
                            ("Brannslukker tilgjengelig", "Sjekk", "OK"),
                            ("Romningsveier frie", "Sjekk", "OK"),
                            ("Kommentar", "Tekst", "Alt i orden"),
                        };

                        foreach (var (label, type, value) in items)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(label);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(type);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(value);
                        }
                    });
                });

                page.Footer().Text("Ref: SJK-12345678").FontSize(7);
            });
        }).GeneratePdf();

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void GenerateSjaPdf_WithHazardsAndParticipants_ProducesValidBytes()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Sikker Jobb Analyse (SJA)").Bold().FontSize(16);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Gravearbeid ved vei").Bold().FontSize(14);
                    col.Item().Text(t => { t.Span("Dato: ").SemiBold(); t.Span("01.04.2026"); });
                    col.Item().Text(t => { t.Span("Sted: ").SemiBold(); t.Span("Hovedgata 42"); });

                    // Hazards
                    col.Item().PaddingTop(10).Text("Faremomenter").SemiBold().FontSize(11);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(60);
                            columns.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Fare").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Sanns.").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Konsekv.").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Risiko").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tiltak").SemiBold();
                        });

                        // High risk
                        table.Cell().Padding(4).Text("Ras i gravesjakt");
                        table.Cell().Padding(4).AlignCenter().Text("4");
                        table.Cell().Padding(4).AlignCenter().Text("5");
                        table.Cell().Padding(4).AlignCenter().Background(Colors.Red.Lighten4).Text("20").SemiBold();
                        table.Cell().Padding(4).Text("Spunting og sperring");

                        // Low risk
                        table.Cell().Padding(4).Text("Stoy fra maskiner");
                        table.Cell().Padding(4).AlignCenter().Text("2");
                        table.Cell().Padding(4).AlignCenter().Text("1");
                        table.Cell().Padding(4).AlignCenter().Background(Colors.Green.Lighten4).Text("2").SemiBold();
                        table.Cell().Padding(4).Text("Hoselklokker");
                    });

                    // Participants
                    col.Item().PaddingTop(10).Text("Deltakere").SemiBold().FontSize(11);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Navn").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Signert").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Signert dato").SemiBold();
                        });

                        table.Cell().Padding(4).Text("Ola Nordmann");
                        table.Cell().Padding(4).Text("Ja");
                        table.Cell().Padding(4).Text("01.04.2026 08:30");

                        table.Cell().Padding(4).Text("Kari Hansen");
                        table.Cell().Padding(4).Text("Nei");
                        table.Cell().Padding(4).Text("-");
                    });
                });

                page.Footer().Text("SJA-12345678").FontSize(7);
            });
        }).GeneratePdf();

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void GenerateHoursExportPdf_LandscapeWithTable_ProducesValidBytes()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Text("Timeliste").Bold().FontSize(16);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Periode: 01.03.2026 - 31.03.2026").Bold().FontSize(12);
                    col.Item().Text(t => { t.Span("Ansatt: ").SemiBold(); t.Span("Ola Nordmann"); });

                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(50);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Dato").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Ansatt").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Prosjekt").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Timer").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Overtid").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Pause").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Kategori").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Status").SemiBold();
                        });

                        table.Cell().Padding(3).Text("01.03.2026");
                        table.Cell().Padding(3).Text("Ola Nordmann");
                        table.Cell().Padding(3).Text("Tunnelprosjektet");
                        table.Cell().Padding(3).AlignRight().Text("7.5");
                        table.Cell().Padding(3).AlignRight().Text("0.0");
                        table.Cell().Padding(3).AlignRight().Text("30 min");
                        table.Cell().Padding(3).Text("Arbeid");
                        table.Cell().Padding(3).Text("Godkjent");
                    });

                    col.Item().PaddingTop(10).AlignRight().Text(t =>
                    {
                        t.Span("Totalt: ").SemiBold();
                        t.Span("7.5 timer");
                    });
                });

                page.Footer().Text("TIM-12345678").FontSize(7);
            });
        }).GeneratePdf();

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void GenerateProjectSummaryPdf_WithMetrics_ProducesValidBytes()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Prosjektsammendrag").Bold().FontSize(16);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("E39 Tunnelprosjektet").Bold().FontSize(16);
                    col.Item().Text(t => { t.Span("Status: ").SemiBold(); t.Span("Aktiv"); });
                    col.Item().Text(t => { t.Span("Kunde: ").SemiBold(); t.Span("Statens vegvesen"); });

                    col.Item().PaddingTop(15).Text("Nokkeltall").SemiBold().FontSize(13);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Padding(8).Background(Colors.Blue.Lighten5).Column(c =>
                        {
                            c.Item().Text("Timer registrert").SemiBold();
                            c.Item().Text("1250.5").FontSize(20).Bold();
                        });

                        table.Cell().Padding(8).Background(Colors.Green.Lighten5).Column(c =>
                        {
                            c.Item().Text("Sjekklister fullfort").SemiBold();
                            c.Item().Text("42").FontSize(20).Bold();
                        });
                    });

                    col.Item().PaddingTop(10).Text("Avvik").SemiBold().FontSize(11);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(80);
                        });

                        table.Cell().Padding(4).Text("Apne");
                        table.Cell().Padding(4).AlignRight().Background(Colors.Red.Lighten4).Text("3").SemiBold();
                        table.Cell().Padding(4).Text("Lukket");
                        table.Cell().Padding(4).AlignRight().Background(Colors.Green.Lighten4).Text("15").SemiBold();
                    });
                });

                page.Footer().Text("PRO-12345678").FontSize(7);
            });
        }).GeneratePdf();

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void GenerateMiniCvPdf_WithCertifications_ProducesValidBytes()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Mini-CV").Bold().FontSize(16);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Ola Nordmann").Bold().FontSize(16);
                    col.Item().Text(t => { t.Span("E-post: ").SemiBold(); t.Span("ola@example.com"); });
                    col.Item().Text(t => { t.Span("Telefon: ").SemiBold(); t.Span("+47 12345678"); });

                    col.Item().PaddingTop(10).Text("Sertifiseringer").SemiBold().FontSize(12);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Navn").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Type").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Utstedt av").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Utloper").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Status").SemiBold();
                        });

                        table.Cell().Padding(3).Text("Fallsikring");
                        table.Cell().Padding(3).Text("HMS");
                        table.Cell().Padding(3).Text("SafeWork AS");
                        table.Cell().Padding(3).Text("15.06.2027");
                        table.Cell().Padding(3).Background(Colors.Green.Lighten4).Text("Gyldig");

                        table.Cell().Padding(3).Text("Varmt arbeid");
                        table.Cell().Padding(3).Text("HMS");
                        table.Cell().Padding(3).Text("Brann AS");
                        table.Cell().Padding(3).Text("01.02.2026");
                        table.Cell().Padding(3).Background(Colors.Red.Lighten4).Text("Utlopt");
                    });
                });

                page.Footer().Text("CV-12345678").FontSize(7);
            });
        }).GeneratePdf();

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void GenerateFullCvPdf_WithTrainingAndSkills_ProducesValidBytes()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Fullstendig CV").Bold().FontSize(16);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Ola Nordmann").Bold().FontSize(18);
                    col.Item().Text(t => { t.Span("E-post: ").SemiBold(); t.Span("ola@example.com"); });

                    col.Item().PaddingTop(10).Text("Bedriftstilhorighet").SemiBold().FontSize(12);
                    col.Item().Text(t => { t.Span("Fjellbygg AS").SemiBold(); t.Span(" (FieldWorker)"); });

                    col.Item().PaddingTop(15).Text("Sertifiseringer").SemiBold().FontSize(12);
                    col.Item().Text("Ingen sertifiseringer registrert.").FontColor(Colors.Grey.Medium);

                    col.Item().PaddingTop(15).Text("Intern opplaering").SemiBold().FontSize(12);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Tema").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Dato").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Instruktor").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Timer").SemiBold();
                        });

                        table.Cell().Padding(3).Text("Stillassikring");
                        table.Cell().Padding(3).Text("15.03.2026");
                        table.Cell().Padding(3).Text("Kari Hansen");
                        table.Cell().Padding(3).Text("4.0");
                    });

                    col.Item().PaddingTop(15).Text("Kompetanseoversikt").SemiBold().FontSize(12);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Padding(8).Background(Colors.Blue.Lighten5).Column(c =>
                        {
                            c.Item().Text("Sertifiseringer").SemiBold();
                            c.Item().Text("0").FontSize(18).Bold();
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Padding(8).Background(Colors.Green.Lighten5).Column(c =>
                        {
                            c.Item().Text("Opplaering").SemiBold();
                            c.Item().Text("1").FontSize(18).Bold();
                        });
                    });
                });

                page.Footer().Text("FCV-12345678").FontSize(7);
            });
        }).GeneratePdf();

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void GenerateEquipmentReportPdf_WithMaintenanceLog_ProducesValidBytes()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Utstyrsrapport").Bold().FontSize(16);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Caterpillar 320").Bold().FontSize(16);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(t => { t.Span("Type: ").SemiBold(); t.Span("Gravemaskin"); });
                            left.Item().Text(t => { t.Span("Merke: ").SemiBold(); t.Span("Caterpillar"); });
                            left.Item().Text(t => { t.Span("Modell: ").SemiBold(); t.Span("320"); });
                        });
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text(t => { t.Span("Reg.nr: ").SemiBold(); t.Span("AB12345"); });
                            right.Item().Text(t => { t.Span("Aktiv: ").SemiBold(); t.Span("Ja"); });
                        });
                    });

                    col.Item().PaddingTop(15).Text("Vedlikeholdslogg").SemiBold().FontSize(12);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Dato").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Beskrivelse").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Utfort av").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Kostnad").SemiBold();
                        });

                        table.Cell().Padding(3).Text("15.03.2026");
                        table.Cell().Padding(3).Text("Oljeskift og filterbytte");
                        table.Cell().Padding(3).Text("Verkstedet AS");
                        table.Cell().Padding(3).AlignRight().Text("4 500 kr");

                        table.Cell().Padding(3).Text("01.02.2026");
                        table.Cell().Padding(3).Text("Belteskifte");
                        table.Cell().Padding(3).Text("Ola Nordmann");
                        table.Cell().Padding(3).AlignRight().Text("85 000 kr");
                    });

                    col.Item().PaddingTop(5).AlignRight().Text(t =>
                    {
                        t.Span("Total vedlikeholdskostnad: ").SemiBold();
                        t.Span("89 500 kr");
                    });
                });

                page.Footer().Text("UTS-12345678").FontSize(7);
            });
        }).GeneratePdf();

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void GeneratedPdf_ContainsValidPdfHeader()
    {
        // Verify all generated PDFs contain the standard PDF magic bytes
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Content().Text("Solodoc test dokument");
            });
        }).GeneratePdf();

        bytes.Should().NotBeEmpty();
        // %PDF magic bytes
        var header = System.Text.Encoding.ASCII.GetString(bytes, 0, 4);
        header.Should().Be("%PDF");
    }

    [Fact]
    public void GeneratedPdf_WithFooterPageNumbers_ProducesValidBytes()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                page.Content().Text("Test innhold");

                page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Generert: ").FontSize(7);
                        text.Span("01.04.2026 10:00:00 UTC").FontSize(7);
                    });
                    row.RelativeItem().AlignCenter().Text(text =>
                    {
                        text.Span("Ref: ").FontSize(7);
                        text.Span("DOC-12345678").FontSize(7);
                    });
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Side ").FontSize(7);
                        text.CurrentPageNumber().FontSize(7);
                        text.Span(" av ").FontSize(7);
                        text.TotalPages().FontSize(7);
                    });
                });
            });
        }).GeneratePdf();

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }
}
