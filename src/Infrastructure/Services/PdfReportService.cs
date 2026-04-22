using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Solodoc.Application.Services;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;

namespace Solodoc.Infrastructure.Services;

public class PdfReportService(SolodocDbContext db, ILogger<PdfReportService> logger) : IPdfReportService
{
    static PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateDeviationReportAsync(Guid deviationId, CancellationToken ct = default)
    {
        var deviation = await db.Deviations
            .Where(d => d.Id == deviationId)
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.Description,
                d.Status,
                d.Severity,
                d.Type,
                d.CorrectiveAction,
                d.CorrectiveActionDeadline,
                d.CorrectiveActionCompletedAt,
                d.Latitude,
                d.Longitude,
                d.InjuryDescription,
                d.BodyPart,
                d.FirstAidGiven,
                d.HospitalVisit,
                d.CreatedAt,
                d.ClosedAt,
                d.TenantId,
                ReporterName = db.Persons.Where(p => p.Id == d.ReportedById).Select(p => p.FullName).FirstOrDefault() ?? "Ukjent",
                AssigneeName = d.AssignedToId != null
                    ? db.Persons.Where(p => p.Id == d.AssignedToId).Select(p => p.FullName).FirstOrDefault()
                    : null,
                ProjectName = d.ProjectId != null
                    ? db.Projects.Where(p => p.Id == d.ProjectId).Select(p => p.Name).FirstOrDefault()
                    : null,
                TenantName = db.Tenants.Where(t => t.Id == d.TenantId).Select(t => t.Name).FirstOrDefault() ?? ""
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Deviation {deviationId} not found.");

        var documentNumber = $"AVV-{deviation.Id.ToString()[..8].ToUpperInvariant()}";
        var generatedAt = DateTimeOffset.UtcNow;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(h => ComposeHeader(h, "Avviksrapport", deviation.TenantName, documentNumber));

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text(deviation.Title).Bold().FontSize(14);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(t => { t.Span("Status: ").SemiBold(); t.Span(FormatDeviationStatus(deviation.Status)); });
                            left.Item().Text(t => { t.Span("Alvorlighetsgrad: ").SemiBold(); t.Span(FormatDeviationSeverity(deviation.Severity)); });
                            if (deviation.Type.HasValue)
                                left.Item().Text(t => { t.Span("Type: ").SemiBold(); t.Span(FormatDeviationType(deviation.Type.Value)); });
                        });
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text(t => { t.Span("Rapportert av: ").SemiBold(); t.Span(deviation.ReporterName); });
                            if (deviation.AssigneeName is not null)
                                right.Item().Text(t => { t.Span("Tildelt: ").SemiBold(); t.Span(deviation.AssigneeName); });
                            if (deviation.ProjectName is not null)
                                right.Item().Text(t => { t.Span("Prosjekt: ").SemiBold(); t.Span(deviation.ProjectName); });
                        });
                    });

                    col.Item().Text(t => { t.Span("Opprettet: ").SemiBold(); t.Span(deviation.CreatedAt.ToString("dd.MM.yyyy HH:mm")); });
                    if (deviation.ClosedAt.HasValue)
                        col.Item().Text(t => { t.Span("Lukket: ").SemiBold(); t.Span(deviation.ClosedAt.Value.ToString("dd.MM.yyyy HH:mm")); });

                    if (!string.IsNullOrWhiteSpace(deviation.Description))
                    {
                        col.Item().PaddingTop(5).Text("Beskrivelse").SemiBold().FontSize(11);
                        col.Item().Text(deviation.Description);
                    }

                    if (!string.IsNullOrWhiteSpace(deviation.CorrectiveAction))
                    {
                        col.Item().PaddingTop(5).Text("Korrigerende tiltak").SemiBold().FontSize(11);
                        col.Item().Text(deviation.CorrectiveAction);
                        if (deviation.CorrectiveActionDeadline.HasValue)
                            col.Item().Text(t => { t.Span("Frist: ").SemiBold(); t.Span(deviation.CorrectiveActionDeadline.Value.ToString("dd.MM.yyyy")); });
                        if (deviation.CorrectiveActionCompletedAt.HasValue)
                            col.Item().Text(t => { t.Span("Fullført: ").SemiBold(); t.Span(deviation.CorrectiveActionCompletedAt.Value.ToString("dd.MM.yyyy")); });
                    }

                    if (!string.IsNullOrWhiteSpace(deviation.InjuryDescription))
                    {
                        col.Item().PaddingTop(5).Text("Personskade").SemiBold().FontSize(11);
                        col.Item().Text(t => { t.Span("Beskrivelse: ").SemiBold(); t.Span(deviation.InjuryDescription); });
                        if (!string.IsNullOrWhiteSpace(deviation.BodyPart))
                            col.Item().Text(t => { t.Span("Kroppsdel: ").SemiBold(); t.Span(deviation.BodyPart); });
                        if (deviation.FirstAidGiven.HasValue)
                            col.Item().Text(t => { t.Span("Forstehjelp gitt: ").SemiBold(); t.Span(deviation.FirstAidGiven.Value ? "Ja" : "Nei"); });
                        if (deviation.HospitalVisit.HasValue)
                            col.Item().Text(t => { t.Span("Sykehusbesøk: ").SemiBold(); t.Span(deviation.HospitalVisit.Value ? "Ja" : "Nei"); });
                    }

                    if (deviation.Latitude.HasValue && deviation.Longitude.HasValue)
                    {
                        col.Item().PaddingTop(5).Text(t =>
                        {
                            t.Span("GPS: ").SemiBold();
                            t.Span($"{deviation.Latitude:F6}, {deviation.Longitude:F6}");
                        });
                    }
                });

                page.Footer().Element(f => ComposeFooter(f, documentNumber, generatedAt));
            });
        }).GeneratePdf();

        logger.LogInformation("Generated deviation report {DocumentNumber} for deviation {DeviationId}", documentNumber, deviationId);
        return bytes;
    }

    public async Task<byte[]> GenerateChecklistReportAsync(Guid instanceId, CancellationToken ct = default)
    {
        var instance = await db.ChecklistInstances
            .Include(i => i.Items)
            .Include(i => i.TemplateVersion)
                .ThenInclude(v => v.ChecklistTemplate)
            .Include(i => i.TemplateVersion)
                .ThenInclude(v => v.Items)
            .Where(i => i.Id == instanceId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"ChecklistInstance {instanceId} not found.");

        var tenantName = await db.Tenants
            .Where(t => t.Id == instance.TenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? "";

        var projectName = instance.ProjectId.HasValue
            ? await db.Projects.Where(p => p.Id == instance.ProjectId).Select(p => p.Name).FirstOrDefaultAsync(ct)
            : null;

        var startedByName = await db.Persons
            .Where(p => p.Id == instance.StartedById)
            .Select(p => p.FullName)
            .FirstOrDefaultAsync(ct) ?? "Ukjent";

        var completedByName = instance.SubmittedById.HasValue
            ? await db.Persons.Where(p => p.Id == instance.SubmittedById).Select(p => p.FullName).FirstOrDefaultAsync(ct)
            : null;

        var templateName = instance.TemplateVersion.ChecklistTemplate.Name;
        var documentNumber = $"SJK-{instance.Id.ToString()[..8].ToUpperInvariant()}";
        var generatedAt = DateTimeOffset.UtcNow;

        var templateItems = instance.TemplateVersion.Items
            .OrderBy(i => i.SortOrder)
            .ToList();

        var instanceItemsByTemplateId = instance.Items.ToDictionary(i => i.TemplateItemId);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(h => ComposeHeader(h, "Sjekkliste", tenantName, documentNumber));

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text(templateName).Bold().FontSize(14);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(t => { t.Span("Status: ").SemiBold(); t.Span(FormatChecklistStatus(instance.Status)); });
                            left.Item().Text(t => { t.Span("Startet av: ").SemiBold(); t.Span(startedByName); });
                        });
                        row.RelativeItem().Column(right =>
                        {
                            if (projectName is not null)
                                right.Item().Text(t => { t.Span("Prosjekt: ").SemiBold(); t.Span(projectName); });
                            if (instance.SubmittedAt.HasValue)
                                right.Item().Text(t => { t.Span("Innsendt: ").SemiBold(); t.Span(instance.SubmittedAt.Value.ToString("dd.MM.yyyy HH:mm")); });
                            if (completedByName is not null)
                                right.Item().Text(t => { t.Span("Innsendt av: ").SemiBold(); t.Span(completedByName); });
                        });
                    });

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

                        foreach (var templateItem in templateItems)
                        {
                            instanceItemsByTemplateId.TryGetValue(templateItem.Id, out var instanceItem);

                            var responseValue = templateItem.Type switch
                            {
                                ChecklistItemType.Check => instanceItem?.CheckValue switch
                                {
                                    true => "OK",
                                    false => "Ikke relevant",
                                    _ => "-"
                                },
                                _ => instanceItem?.Value ?? "-"
                            };

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(templateItem.Label);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(FormatItemType(templateItem.Type));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(responseValue);
                        }
                    });
                });

                page.Footer().Element(f => ComposeFooter(f, documentNumber, generatedAt));
            });
        }).GeneratePdf();

        logger.LogInformation("Generated checklist report {DocumentNumber} for instance {InstanceId}", documentNumber, instanceId);
        return bytes;
    }

    public async Task<byte[]> GenerateSjaReportAsync(Guid sjaId, CancellationToken ct = default)
    {
        var sja = await db.SjaForms
            .Include(s => s.Hazards.OrderBy(h => h.SortOrder))
            .Include(s => s.Participants)
            .Where(s => s.Id == sjaId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"SjaForm {sjaId} not found.");

        var tenantName = await db.Tenants
            .Where(t => t.Id == sja.TenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? "";

        var projectName = sja.ProjectId.HasValue
            ? await db.Projects.Where(p => p.Id == sja.ProjectId).Select(p => p.Name).FirstOrDefaultAsync(ct)
            : null;

        var createdByName = await db.Persons
            .Where(p => p.Id == sja.CreatedById)
            .Select(p => p.FullName)
            .FirstOrDefaultAsync(ct) ?? "Ukjent";

        var participantPersonIds = sja.Participants.Select(p => p.PersonId).ToList();
        var personNames = await db.Persons
            .Where(p => participantPersonIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var documentNumber = $"SJA-{sja.Id.ToString()[..8].ToUpperInvariant()}";
        var generatedAt = DateTimeOffset.UtcNow;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(h => ComposeHeader(h, "Sikker Jobb Analyse (SJA)", tenantName, documentNumber));

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text(sja.Title).Bold().FontSize(14);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(t => { t.Span("Status: ").SemiBold(); t.Span(sja.Status); });
                            left.Item().Text(t => { t.Span("Dato: ").SemiBold(); t.Span(sja.Date.ToString("dd.MM.yyyy")); });
                            left.Item().Text(t => { t.Span("Opprettet av: ").SemiBold(); t.Span(createdByName); });
                        });
                        row.RelativeItem().Column(right =>
                        {
                            if (sja.Location is not null)
                                right.Item().Text(t => { t.Span("Sted: ").SemiBold(); t.Span(sja.Location); });
                            if (projectName is not null)
                                right.Item().Text(t => { t.Span("Prosjekt: ").SemiBold(); t.Span(projectName); });
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(sja.Description))
                    {
                        col.Item().PaddingTop(5).Text("Beskrivelse").SemiBold().FontSize(11);
                        col.Item().Text(sja.Description);
                    }

                    if (sja.Hazards.Count > 0)
                    {
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

                            foreach (var hazard in sja.Hazards)
                            {
                                var riskColor = hazard.RiskScore switch
                                {
                                    >= 15 => Colors.Red.Lighten4,
                                    >= 8 => Colors.Orange.Lighten4,
                                    >= 4 => Colors.Yellow.Lighten4,
                                    _ => Colors.Green.Lighten4
                                };

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(hazard.Description);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(hazard.Probability.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(hazard.Consequence.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter()
                                    .Background(riskColor).Text(hazard.RiskScore.ToString()).SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(hazard.Mitigation ?? "-");
                            }
                        });
                    }

                    if (sja.Participants.Count > 0)
                    {
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

                            foreach (var participant in sja.Participants)
                            {
                                var name = participant.IsExternal ? (participant.ExternalName ?? "Ukjent") : (participant.PersonId.HasValue ? personNames.GetValueOrDefault(participant.PersonId.Value, "Ukjent") : "Ukjent");
                                var hasSigned = participant.SignedAt.HasValue;

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(name);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(hasSigned ? "Ja" : "Nei");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(participant.SignedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-");
                            }
                        });
                    }
                });

                page.Footer().Element(f => ComposeFooter(f, documentNumber, generatedAt));
            });
        }).GeneratePdf();

        logger.LogInformation("Generated SJA report {DocumentNumber} for SJA {SjaId}", documentNumber, sjaId);
        return bytes;
    }

    public async Task<byte[]> GenerateHoursExportAsync(Guid? projectId, Guid? personId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var query = db.TimeEntries.AsQueryable();

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
        if (personId.HasValue)
            query = query.Where(t => t.PersonId == personId.Value);

        query = query.Where(t => t.Date >= from && t.Date <= to);

        var entries = await query
            .OrderBy(t => t.Date)
            .ThenBy(t => t.ClockIn)
            .Select(t => new
            {
                t.Date,
                t.Hours,
                t.OvertimeHours,
                t.Category,
                t.Status,
                t.Notes,
                t.BreakMinutes,
                PersonName = db.Persons.Where(p => p.Id == t.PersonId).Select(p => p.FullName).FirstOrDefault() ?? "Ukjent",
                ProjectName = t.ProjectId != null
                    ? db.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).FirstOrDefault()
                    : null,
                JobDescription = t.JobId != null
                    ? db.Jobs.Where(j => j.Id == t.JobId).Select(j => j.Description).FirstOrDefault()
                    : null
            })
            .ToListAsync(ct);

        var tenantName = "";
        if (projectId.HasValue)
        {
            tenantName = await db.Projects
                .Where(p => p.Id == projectId.Value)
                .Select(p => db.Tenants.Where(t => t.Id == p.TenantId).Select(t => t.Name).FirstOrDefault() ?? "")
                .FirstOrDefaultAsync(ct) ?? "";
        }

        var documentNumber = $"TIM-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        var generatedAt = DateTimeOffset.UtcNow;

        var totalHours = entries.Sum(e => e.Hours);
        var totalOvertime = entries.Sum(e => e.OvertimeHours);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(h => ComposeHeader(h, "Timeliste", tenantName, documentNumber));

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text($"Periode: {from:dd.MM.yyyy} - {to:dd.MM.yyyy}").Bold().FontSize(12);

                    if (personId.HasValue && entries.Count > 0)
                        col.Item().Text(t => { t.Span("Ansatt: ").SemiBold(); t.Span(entries[0].PersonName); });

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
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Prosjekt/Oppdrag").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Timer").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Overtid").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Pause").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Kategori").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Status").SemiBold();
                        });

                        foreach (var entry in entries)
                        {
                            var target = entry.ProjectName ?? entry.JobDescription ?? "-";

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(entry.Date.ToString("dd.MM.yyyy"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(entry.PersonName);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(target);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(entry.Hours.ToString("F1"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(entry.OvertimeHours.ToString("F1"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{entry.BreakMinutes} min");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(FormatTimeEntryCategory(entry.Category));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(FormatTimeEntryStatus(entry.Status));
                        }
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Totalt: ").SemiBold();
                            t.Span($"{totalHours:F1} timer");
                            if (totalOvertime > 0)
                            {
                                t.Span($" (herav {totalOvertime:F1} overtid)");
                            }
                        });
                    });

                    col.Item().Text(t => { t.Span("Antall registreringer: ").SemiBold(); t.Span(entries.Count.ToString()); });
                });

                page.Footer().Element(f => ComposeFooter(f, documentNumber, generatedAt));
            });
        }).GeneratePdf();

        logger.LogInformation("Generated hours export {DocumentNumber} for period {From} to {To}", documentNumber, from, to);
        return bytes;
    }

    public async Task<byte[]> GenerateProjectSummaryAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await db.Projects
            .Where(p => p.Id == projectId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        var tenantName = await db.Tenants
            .Where(t => t.Id == project.TenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? "";

        var totalHours = await db.TimeEntries
            .Where(t => t.ProjectId == projectId)
            .SumAsync(t => t.Hours, ct);

        var deviationsByStatus = await db.Deviations
            .Where(d => d.ProjectId == projectId)
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var checklistsCompleted = await db.ChecklistInstances
            .Where(c => c.ProjectId == projectId && (c.Status == ChecklistInstanceStatus.Submitted || c.Status == ChecklistInstanceStatus.Approved))
            .CountAsync(ct);

        var checklistsInProgress = await db.ChecklistInstances
            .Where(c => c.ProjectId == projectId && c.Status == ChecklistInstanceStatus.Draft)
            .CountAsync(ct);

        var documentNumber = $"PRO-{project.Id.ToString()[..8].ToUpperInvariant()}";
        var generatedAt = DateTimeOffset.UtcNow;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(h => ComposeHeader(h, "Prosjektsammendrag", tenantName, documentNumber));

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text(project.Name).Bold().FontSize(16);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(t => { t.Span("Status: ").SemiBold(); t.Span(FormatProjectStatus(project.Status)); });
                            if (project.ClientName is not null)
                                left.Item().Text(t => { t.Span("Kunde: ").SemiBold(); t.Span(project.ClientName); });
                            if (project.Address is not null)
                                left.Item().Text(t => { t.Span("Adresse: ").SemiBold(); t.Span(project.Address); });
                        });
                        row.RelativeItem().Column(right =>
                        {
                            if (project.StartDate.HasValue)
                                right.Item().Text(t => { t.Span("Startdato: ").SemiBold(); t.Span(project.StartDate.Value.ToString("dd.MM.yyyy")); });
                            if (project.PlannedEndDate.HasValue)
                                right.Item().Text(t => { t.Span("Planlagt slutt: ").SemiBold(); t.Span(project.PlannedEndDate.Value.ToString("dd.MM.yyyy")); });
                            if (project.EstimatedHours.HasValue)
                                right.Item().Text(t => { t.Span("Estimerte timer: ").SemiBold(); t.Span(project.EstimatedHours.Value.ToString("F0")); });
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(project.Description))
                    {
                        col.Item().PaddingTop(5).Text("Beskrivelse").SemiBold().FontSize(11);
                        col.Item().Text(project.Description);
                    }

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
                            c.Item().Text(totalHours.ToString("F1")).FontSize(20).Bold();
                            if (project.EstimatedHours.HasValue && project.EstimatedHours.Value > 0)
                            {
                                var pct = totalHours / project.EstimatedHours.Value * 100;
                                c.Item().Text($"{pct:F0}% av estimat").FontSize(9).FontColor(Colors.Grey.Medium);
                            }
                        });

                        table.Cell().Padding(8).Background(Colors.Green.Lighten5).Column(c =>
                        {
                            c.Item().Text("Sjekklister fullført").SemiBold();
                            c.Item().Text(checklistsCompleted.ToString()).FontSize(20).Bold();
                            c.Item().Text($"{checklistsInProgress} under arbeid").FontSize(9).FontColor(Colors.Grey.Medium);
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

                        var openCount = deviationsByStatus.FirstOrDefault(d => d.Status == DeviationStatus.Open)?.Count ?? 0;
                        var inProgressCount = deviationsByStatus.FirstOrDefault(d => d.Status == DeviationStatus.InProgress)?.Count ?? 0;
                        var closedCount = deviationsByStatus.FirstOrDefault(d => d.Status == DeviationStatus.Closed)?.Count ?? 0;

                        table.Cell().Padding(4).Text("Apne");
                        table.Cell().Padding(4).AlignRight().Background(Colors.Red.Lighten4).Text(openCount.ToString()).SemiBold();
                        table.Cell().Padding(4).Text("Under behandling");
                        table.Cell().Padding(4).AlignRight().Background(Colors.Orange.Lighten4).Text(inProgressCount.ToString()).SemiBold();
                        table.Cell().Padding(4).Text("Lukket");
                        table.Cell().Padding(4).AlignRight().Background(Colors.Green.Lighten4).Text(closedCount.ToString()).SemiBold();
                    });
                });

                page.Footer().Element(f => ComposeFooter(f, documentNumber, generatedAt));
            });
        }).GeneratePdf();

        logger.LogInformation("Generated project summary {DocumentNumber} for project {ProjectId}", documentNumber, projectId);
        return bytes;
    }

    public async Task<byte[]> GenerateMiniCvAsync(Guid personId, CancellationToken ct = default)
    {
        var person = await db.Persons
            .Where(p => p.Id == personId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Person {personId} not found.");

        var certifications = await db.EmployeeCertifications
            .Where(c => c.PersonId == personId)
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync(ct);

        var documentNumber = $"CV-{person.Id.ToString()[..8].ToUpperInvariant()}";
        var generatedAt = DateTimeOffset.UtcNow;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(h => ComposeHeader(h, "Mini-CV", "", documentNumber));

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text(person.FullName).Bold().FontSize(16);
                    col.Item().Text(t => { t.Span("E-post: ").SemiBold(); t.Span(person.Email); });
                    if (!string.IsNullOrWhiteSpace(person.PhoneNumber))
                        col.Item().Text(t => { t.Span("Telefon: ").SemiBold(); t.Span(person.PhoneNumber); });

                    col.Item().PaddingTop(10).Text("Sertifiseringer").SemiBold().FontSize(12);

                    if (certifications.Count == 0)
                    {
                        col.Item().Text("Ingen sertifiseringer registrert.").FontColor(Colors.Grey.Medium);
                    }
                    else
                    {
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

                            foreach (var cert in certifications)
                            {
                                var statusColor = cert.IsExpired ? Colors.Red.Lighten4
                                    : cert.IsExpiringSoon ? Colors.Orange.Lighten4
                                    : Colors.Green.Lighten4;
                                var statusText = cert.IsExpired ? "Utlopt"
                                    : cert.IsExpiringSoon ? "Snart"
                                    : "Gyldig";

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(cert.Name);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(cert.Type);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(cert.IssuedBy ?? "-");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(cert.ExpiryDate?.ToString("dd.MM.yyyy") ?? "Ingen utløpsdato");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Background(statusColor).Text(statusText);
                            }
                        });
                    }
                });

                page.Footer().Element(f => ComposeFooter(f, documentNumber, generatedAt));
            });
        }).GeneratePdf();

        logger.LogInformation("Generated mini-CV {DocumentNumber} for person {PersonId}", documentNumber, personId);
        return bytes;
    }

    public async Task<byte[]> GenerateFullCvAsync(Guid personId, CancellationToken ct = default)
    {
        var person = await db.Persons
            .Where(p => p.Id == personId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Person {personId} not found.");

        var certifications = await db.EmployeeCertifications
            .Where(c => c.PersonId == personId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.ExpiryDate)
            .ToListAsync(ct);

        var trainings = await db.InternalTrainings
            .Where(t => t.TraineeId == personId)
            .OrderByDescending(t => t.Date)
            .Select(t => new
            {
                t.Topic,
                t.Date,
                t.DurationHours,
                t.Notes,
                TrainerName = db.Persons.Where(p => p.Id == t.TrainerId).Select(p => p.FullName).FirstOrDefault() ?? "Ukjent"
            })
            .ToListAsync(ct);

        var tenantMemberships = await db.TenantMemberships
            .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active)
            .Select(m => new { m.Tenant.Name, m.Role })
            .ToListAsync(ct);

        var documentNumber = $"FCV-{person.Id.ToString()[..8].ToUpperInvariant()}";
        var generatedAt = DateTimeOffset.UtcNow;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(h => ComposeHeader(h, "Fullstendig CV", "", documentNumber));

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text(person.FullName).Bold().FontSize(18);
                    col.Item().Text(t => { t.Span("E-post: ").SemiBold(); t.Span(person.Email); });
                    if (!string.IsNullOrWhiteSpace(person.PhoneNumber))
                        col.Item().Text(t => { t.Span("Telefon: ").SemiBold(); t.Span(person.PhoneNumber); });

                    if (tenantMemberships.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Bedriftstilhorighet").SemiBold().FontSize(12);
                        foreach (var membership in tenantMemberships)
                        {
                            col.Item().Text(t =>
                            {
                                t.Span(membership.Name).SemiBold();
                                t.Span($" ({membership.Role})");
                            });
                        }
                    }

                    col.Item().PaddingTop(15).Text("Sertifiseringer").SemiBold().FontSize(12);

                    if (certifications.Count == 0)
                    {
                        col.Item().Text("Ingen sertifiseringer registrert.").FontColor(Colors.Grey.Medium);
                    }
                    else
                    {
                        var certGroups = certifications.GroupBy(c => c.Type);
                        foreach (var group in certGroups)
                        {
                            col.Item().PaddingTop(5).Text(group.Key).SemiBold().FontSize(11);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Navn").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Utstedt av").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Utloper").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Status").SemiBold();
                                });

                                foreach (var cert in group)
                                {
                                    var statusColor = cert.IsExpired ? Colors.Red.Lighten4
                                        : cert.IsExpiringSoon ? Colors.Orange.Lighten4
                                        : Colors.Green.Lighten4;
                                    var statusText = cert.IsExpired ? "Utlopt"
                                        : cert.IsExpiringSoon ? "Snart"
                                        : "Gyldig";

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(cert.Name);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(cert.IssuedBy ?? "-");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(cert.ExpiryDate?.ToString("dd.MM.yyyy") ?? "Ingen utløpsdato");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Background(statusColor).Text(statusText);
                                }
                            });
                        }
                    }

                    col.Item().PaddingTop(15).Text("Intern opplaering").SemiBold().FontSize(12);

                    if (trainings.Count == 0)
                    {
                        col.Item().Text("Ingen intern opplaering registrert.").FontColor(Colors.Grey.Medium);
                    }
                    else
                    {
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

                            foreach (var training in trainings)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(training.Topic);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(training.Date.ToString("dd.MM.yyyy"));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(training.TrainerName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(training.DurationHours?.ToString("F1") ?? "-");
                            }
                        });
                    }

                    col.Item().PaddingTop(15).Text("Kompetanseoversikt").SemiBold().FontSize(12);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Padding(8).Background(Colors.Blue.Lighten5).Column(c =>
                        {
                            c.Item().Text("Sertifiseringer").SemiBold();
                            c.Item().Text(certifications.Count.ToString()).FontSize(18).Bold();
                            var validCount = certifications.Count(cert => !cert.IsExpired);
                            c.Item().Text($"{validCount} gyldige").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Padding(8).Background(Colors.Green.Lighten5).Column(c =>
                        {
                            c.Item().Text("Opplaering").SemiBold();
                            c.Item().Text(trainings.Count.ToString()).FontSize(18).Bold();
                            var totalTrainingHours = trainings.Sum(t => t.DurationHours ?? 0);
                            c.Item().Text($"{totalTrainingHours:F0} timer totalt").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });
                });

                page.Footer().Element(f => ComposeFooter(f, documentNumber, generatedAt));
            });
        }).GeneratePdf();

        logger.LogInformation("Generated full CV {DocumentNumber} for person {PersonId}", documentNumber, personId);
        return bytes;
    }

    public async Task<byte[]> GenerateEquipmentReportAsync(Guid equipmentId, CancellationToken ct = default)
    {
        var equipment = await db.Equipment
            .Include(e => e.MaintenanceRecords.OrderByDescending(m => m.Date))
            .Where(e => e.Id == equipmentId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Equipment {equipmentId} not found.");

        var tenantName = await db.Tenants
            .Where(t => t.Id == equipment.TenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? "";

        var performerIds = equipment.MaintenanceRecords
            .Where(m => m.PerformedById.HasValue)
            .Select(m => m.PerformedById!.Value)
            .Distinct()
            .ToList();
        var performerNames = performerIds.Count > 0
            ? await db.Persons.Where(p => performerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct)
            : new Dictionary<Guid, string>();

        var documentNumber = $"UTS-{equipment.Id.ToString()[..8].ToUpperInvariant()}";
        var generatedAt = DateTimeOffset.UtcNow;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(h => ComposeHeader(h, "Utstyrsrapport", tenantName, documentNumber));

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text(equipment.Name).Bold().FontSize(16);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            if (equipment.Type is not null)
                                left.Item().Text(t => { t.Span("Type: ").SemiBold(); t.Span(equipment.Type); });
                            if (equipment.Make is not null)
                                left.Item().Text(t => { t.Span("Merke: ").SemiBold(); t.Span(equipment.Make); });
                            if (equipment.Model is not null)
                                left.Item().Text(t => { t.Span("Modell: ").SemiBold(); t.Span(equipment.Model); });
                            if (equipment.Year.HasValue)
                                left.Item().Text(t => { t.Span("Arsmodell: ").SemiBold(); t.Span(equipment.Year.Value.ToString()); });
                        });
                        row.RelativeItem().Column(right =>
                        {
                            if (equipment.RegistrationNumber is not null)
                                right.Item().Text(t => { t.Span("Reg.nr: ").SemiBold(); t.Span(equipment.RegistrationNumber); });
                            if (equipment.SerialNumber is not null)
                                right.Item().Text(t => { t.Span("Serienr: ").SemiBold(); t.Span(equipment.SerialNumber); });
                            right.Item().Text(t => { t.Span("Aktiv: ").SemiBold(); t.Span(equipment.IsActive ? "Ja" : "Nei"); });
                        });
                    });

                    col.Item().PaddingTop(15).Text("Vedlikeholdslogg").SemiBold().FontSize(12);

                    if (equipment.MaintenanceRecords.Count == 0)
                    {
                        col.Item().Text("Ingen vedlikeholdsregistreringer.").FontColor(Colors.Grey.Medium);
                    }
                    else
                    {
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

                            foreach (var record in equipment.MaintenanceRecords)
                            {
                                var performerName = record.PerformedById.HasValue
                                    ? performerNames.GetValueOrDefault(record.PerformedById.Value, "Ukjent")
                                    : "-";

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(record.Date.ToString("dd.MM.yyyy"));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(record.Description);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(performerName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight()
                                    .Text(record.Cost.HasValue ? $"{record.Cost.Value:N0} kr" : "-");
                            }
                        });

                        var totalCost = equipment.MaintenanceRecords.Sum(m => m.Cost ?? 0);
                        if (totalCost > 0)
                        {
                            col.Item().PaddingTop(5).AlignRight().Text(t =>
                            {
                                t.Span("Total vedlikeholdskostnad: ").SemiBold();
                                t.Span($"{totalCost:N0} kr");
                            });
                        }
                    }
                });

                page.Footer().Element(f => ComposeFooter(f, documentNumber, generatedAt));
            });
        }).GeneratePdf();

        logger.LogInformation("Generated equipment report {DocumentNumber} for equipment {EquipmentId}", documentNumber, equipmentId);
        return bytes;
    }

    // --- Shared layout components ---

    private static void ComposeHeader(IContainer container, string reportTitle, string tenantName, string documentNumber)
    {
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Solodoc").Bold().FontSize(16);
                if (!string.IsNullOrWhiteSpace(tenantName))
                    col.Item().Text(tenantName).FontSize(10).FontColor(Colors.Grey.Medium);
                col.Item().Text(reportTitle).FontSize(10).FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(120).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text(text =>
                {
                    text.Span("Dokument: ").FontSize(8);
                    text.Span(documentNumber).Bold().FontSize(8);
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container, string documentNumber, DateTimeOffset generatedAt)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Generert: ").FontSize(7);
                text.Span(generatedAt.ToString("dd.MM.yyyy HH:mm:ss UTC")).FontSize(7);
            });
            row.RelativeItem().AlignCenter().Text(text =>
            {
                text.Span("Ref: ").FontSize(7);
                text.Span(documentNumber).FontSize(7);
            });
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Side ").FontSize(7);
                text.CurrentPageNumber().FontSize(7);
                text.Span(" av ").FontSize(7);
                text.TotalPages().FontSize(7);
            });
        });
    }

    // --- Format helpers ---

    private static string FormatDeviationStatus(DeviationStatus status) => status switch
    {
        DeviationStatus.Open => "Åpen",
        DeviationStatus.InProgress => "Under behandling",
        DeviationStatus.Closed => "Lukket",
        _ => status.ToString()
    };

    private static string FormatDeviationSeverity(DeviationSeverity severity) => severity switch
    {
        DeviationSeverity.Low => "Lav",
        DeviationSeverity.Medium => "Middels",
        DeviationSeverity.High => "Høy",
        _ => severity.ToString()
    };

    private static string FormatDeviationType(DeviationType type) => type switch
    {
        DeviationType.MateriellSkade => "Materiell skade",
        DeviationType.Personskade => "Personskade",
        DeviationType.Nestenulykke => "Nestenulykke",
        DeviationType.FarligTilstand => "Farlig tilstand",
        DeviationType.Kvalitetsavvik => "Kvalitetsavvik",
        DeviationType.Miljøavvik => "Miljøavvik",
        _ => type.ToString()
    };

    private static string FormatProjectStatus(ProjectStatus status) => status switch
    {
        ProjectStatus.Planlagt => "Planlagt",
        ProjectStatus.Active => "Aktiv",
        ProjectStatus.Completed => "Fullført",
        ProjectStatus.Cancelled => "Kansellert",
        _ => status.ToString()
    };

    private static string FormatChecklistStatus(ChecklistInstanceStatus status) => status switch
    {
        ChecklistInstanceStatus.Draft => "Utkast",
        ChecklistInstanceStatus.Submitted => "Innsendt",
        ChecklistInstanceStatus.Approved => "Godkjent",
        ChecklistInstanceStatus.Reopened => "Gjenåpnet",
        _ => status.ToString()
    };

    private static string FormatItemType(ChecklistItemType type) => type switch
    {
        ChecklistItemType.Check => "Sjekk",
        ChecklistItemType.TextInput => "Tekst",
        ChecklistItemType.NumberInput => "Tall",
        ChecklistItemType.DateInput => "Dato",
        ChecklistItemType.Dropdown => "Nedtrekk",
        ChecklistItemType.Photo => "Foto",
        ChecklistItemType.Signature => "Signatur",
        _ => type.ToString()
    };

    private static string FormatTimeEntryCategory(TimeEntryCategory category) => category switch
    {
        TimeEntryCategory.Arbeid => "Arbeid",
        TimeEntryCategory.Reise => "Reise",
        TimeEntryCategory.Kontorarbeid => "Kontor",
        TimeEntryCategory.Lagerarbeid => "Lager",
        TimeEntryCategory.Kurs => "Kurs",
        TimeEntryCategory.Annet => "Annet",
        _ => category.ToString()
    };

    private static string FormatTimeEntryStatus(TimeEntryStatus status) => status switch
    {
        TimeEntryStatus.Draft => "Utkast",
        TimeEntryStatus.Submitted => "Innsendt",
        TimeEntryStatus.Approved => "Godkjent",
        TimeEntryStatus.Rejected => "Avvist",
        _ => status.ToString()
    };
}
