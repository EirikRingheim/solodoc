using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Solodoc.Application.Auth;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Entities.Deviations;
using Solodoc.Domain.Entities.Equipment;
using Solodoc.Domain.Entities.Help;
using Solodoc.Domain.Entities.Projects;
using Solodoc.Domain.Enums;

namespace Solodoc.Infrastructure.Persistence;

public class SeedDataService(
    SolodocDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<SeedDataService> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Tenants.AnyAsync(ct))
        {
            // Ensure admin has SuperAdmin system role
            var existingAdmin = await db.Persons.FirstOrDefaultAsync(p => p.Email == "admin@solodoc.dev", ct);
            if (existingAdmin is not null && existingAdmin.SystemRole is null)
            {
                existingAdmin.SystemRole = SystemRole.SuperAdmin;
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Set SuperAdmin role on admin@solodoc.dev");
            }
            // Ensure Solotrial365 coupon exists
            if (!await db.CouponCodes.AnyAsync(c => c.Code == "SOLOTRIAL365", ct))
            {
                db.CouponCodes.Add(new Solodoc.Domain.Entities.Billing.CouponCode
                {
                    Code = "SOLOTRIAL365",
                    Description = "1 ar gratis proveperiode for beta-testere",
                    TrialDays = 365,
                    MaxRedemptions = 0, // unlimited
                    IsActive = true
                });
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Created SOLOTRIAL365 coupon code");
            }

            // Ensure 2026 travel expense rates exist for all tenants
            var tenantIds = await db.Tenants.Where(t => !t.IsDeleted).Select(t => t.Id).ToListAsync(ct);
            foreach (var tid in tenantIds)
            {
                if (!await db.TravelExpenseRates.IgnoreQueryFilters().AnyAsync(r => r.TenantId == tid && r.Year == 2026, ct))
                {
                    db.TravelExpenseRates.Add(new Solodoc.Domain.Entities.Expenses.TravelExpenseRate
                    {
                        TenantId = tid, Year = 2026,
                        Diet6To12Hours = 397, Diet12PlusHours = 736, DietOvernight = 1012,
                        BreakfastDeductionPct = 20, LunchDeductionPct = 30, DinnerDeductionPct = 50,
                        MileagePerKm = 5.30m, PassengerSurchargePerKm = 1.00m,
                        ForestRoadSurchargePerKm = 1.00m, TrailerSurchargePerKm = 1.00m,
                        UndocumentedNightRate = 452
                    });
                }
                if (!await db.ExpenseSettingsTable.IgnoreQueryFilters().AnyAsync(s => s.TenantId == tid, ct))
                {
                    db.ExpenseSettingsTable.Add(new Solodoc.Domain.Entities.Expenses.ExpenseSettings
                    {
                        TenantId = tid, RequireDate = true
                    });
                }
            }
            // Ensure default equipment type categories exist for all tenants
            foreach (var tid in tenantIds)
            {
                if (!await db.EquipmentTypeCategories.IgnoreQueryFilters().AnyAsync(c => c.TenantId == tid, ct))
                {
                    var eqTypes = new (string Name, int Sort)[]
                    {
                        ("Bil", 1), ("Lastebil", 2), ("Varebil", 3), ("Traktor", 4),
                        ("Gravemaskin", 5), ("Hjullaster", 6), ("Lift", 7), ("Stillas", 8),
                        ("Håndverktøy", 9), ("Elektroverktøy", 10), ("Måleinstrument", 11),
                        ("Verneutstyr", 12), ("Tilhenger", 13), ("Generator", 14), ("Annet", 99)
                    };
                    foreach (var (name, sort) in eqTypes)
                        db.EquipmentTypeCategories.Add(new EquipmentTypeCategory
                            { TenantId = tid, Name = name, SortOrder = sort, IsDefault = true, IsActive = true });
                    logger.LogInformation("Seeded default equipment type categories for tenant {TenantId}", tid);
                }

                if (!await db.DeviationCategories.IgnoreQueryFilters().AnyAsync(c => c.TenantId == tid, ct))
                {
                    var devCats = new (string Name, int Sort)[]
                    {
                        ("Personskade", 1), ("Nestenulykke", 2), ("Materiell skade", 3),
                        ("Farlig tilstand", 4), ("Kvalitetsavvik", 5), ("Miljøavvik", 6),
                        ("Sikkerhet", 7), ("Brann/eksplosjon", 8), ("Kjemikalie/gass", 9),
                        ("Ergonomi", 10), ("Annet", 99)
                    };
                    foreach (var (name, sort) in devCats)
                        db.DeviationCategories.Add(new DeviationCategory
                            { TenantId = tid, Name = name, SortOrder = sort, IsDefault = true, IsActive = true });
                    logger.LogInformation("Seeded default deviation categories for tenant {TenantId}", tid);
                }
            }

            await db.SaveChangesAsync(ct);

            logger.LogInformation("Seed data already exists, skipping");
            return;
        }

        logger.LogInformation("Seeding development data...");

        // Tenant
        var tenant = new Tenant
        {
            Id = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001"),
            Name = "Fjellbygg AS",
            OrgNumber = "999888777",
            BusinessType = BusinessType.AS,
            BusinessAddress = "Fjellveien 42, 5003 Bergen",
            DefaultTimeZoneId = "Europe/Oslo",
            AccentColor = "#4361EE"
        };
        db.Tenants.Add(tenant);

        // Admin user
        var admin = new Person
        {
            Id = Guid.Parse("b1b2c3d4-0000-0000-0000-000000000001"),
            Email = "admin@solodoc.dev",
            FullName = "Admin Fjellbygg",
            PasswordHash = passwordHasher.Hash("Admin1234!"),
            State = PersonState.Active,
            EmailVerified = true,
            TimeZoneId = "Europe/Oslo",
            SystemRole = SystemRole.SuperAdmin
        };
        db.Persons.Add(admin);

        var adminMembership = new TenantMembership
        {
            PersonId = admin.Id,
            TenantId = tenant.Id,
            Role = TenantRole.TenantAdmin,
            State = TenantMembershipState.Active
        };
        db.TenantMemberships.Add(adminMembership);

        // Field worker user
        var bruker = new Person
        {
            Id = Guid.Parse("b1b2c3d4-0000-0000-0000-000000000002"),
            Email = "bruker@solodoc.dev",
            FullName = "Kåre Fjellmann",
            PasswordHash = passwordHasher.Hash("Bruker1234!"),
            State = PersonState.Active,
            EmailVerified = true,
            TimeZoneId = "Europe/Oslo"
        };
        db.Persons.Add(bruker);

        var brukerMembership = new TenantMembership
        {
            PersonId = bruker.Id,
            TenantId = tenant.Id,
            Role = TenantRole.FieldWorker,
            State = TenantMembershipState.Active
        };
        db.TenantMemberships.Add(brukerMembership);

        // Second tenant
        var tenant2 = new Tenant
        {
            Id = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000002"),
            Name = "Vestland Maskin AS",
            OrgNumber = "999888666",
            BusinessType = BusinessType.AS,
            BusinessAddress = "Maskinveien 7, 5200 Os",
            DefaultTimeZoneId = "Europe/Oslo",
            AccentColor = "#E63946"
        };
        db.Tenants.Add(tenant2);

        // Admin is also tenant-admin of second tenant
        var adminMembership2 = new TenantMembership
        {
            PersonId = admin.Id,
            TenantId = tenant2.Id,
            Role = TenantRole.TenantAdmin,
            State = TenantMembershipState.Active
        };
        db.TenantMemberships.Add(adminMembership2);

        // Projects
        var project1 = new Project
        {
            Id = Guid.Parse("c1c2c3d4-0000-0000-0000-000000000001"),
            TenantId = tenant.Id,
            Name = "Nybygg Sentrum",
            Description = "Nybygg av kontorbygg i Bergen sentrum, 6 etasjer.",
            Status = ProjectStatus.Active,
            ClientName = "Bergen Kommune",
            StartDate = new DateOnly(2026, 1, 15),
            Address = "Strandgaten 15, 5003 Bergen",
            QrCodeSlug = "nybygg-sentrum"
        };

        var project2 = new Project
        {
            Id = Guid.Parse("c1c2c3d4-0000-0000-0000-000000000002"),
            TenantId = tenant.Id,
            Name = "Bru Finse",
            Description = "Rehabilitering av gangbru over Finseelva.",
            Status = ProjectStatus.Active,
            ClientName = "Statens vegvesen",
            StartDate = new DateOnly(2025, 9, 1),
            Address = "Finse, 5765 Ulvik",
            QrCodeSlug = "bru-finse"
        };

        var project3 = new Project
        {
            Id = Guid.Parse("c1c2c3d4-0000-0000-0000-000000000003"),
            TenantId = tenant.Id,
            Name = "E6 Utvidelse",
            Description = "Utvidelse av E6 mellom Trondheim og Stjørdal, 12 km strekning.",
            Status = ProjectStatus.Active,
            ClientName = "Nye Veier AS",
            StartDate = new DateOnly(2026, 3, 10),
            Address = "E6, 7070 Bosberg",
            QrCodeSlug = "e6-utvidelse"
        };

        db.Projects.AddRange(project1, project2, project3);

        // Deviations
        var deviations = new[]
        {
            new Deviation
            {
                TenantId = tenant.Id,
                ProjectId = project1.Id,
                Title = "Manglende rekkverk i 3. etasje",
                Description = "Midlertidig rekkverk langs trappesjakt i 3. etasje er ikke montert. Fallfare.",
                Status = DeviationStatus.Open,
                Severity = DeviationSeverity.High,
                ReportedById = bruker.Id
            },
            new Deviation
            {
                TenantId = tenant.Id,
                ProjectId = project1.Id,
                Title = "Feil betongblanding levert",
                Description = "Betongbil leverte B30 i stedet for B45. Støp stoppet inntil riktig blanding ankommer.",
                Status = DeviationStatus.Open,
                Severity = DeviationSeverity.High,
                ReportedById = admin.Id,
                AssignedToId = bruker.Id
            },
            new Deviation
            {
                TenantId = tenant.Id,
                ProjectId = project2.Id,
                Title = "Råteskade i bærebjelke",
                Description = "Oppdaget råte i eksisterende bærebjelke ved søndre feste. Krever ekstra utskifting.",
                Status = DeviationStatus.InProgress,
                Severity = DeviationSeverity.Medium,
                ReportedById = bruker.Id,
                AssignedToId = admin.Id
            },
            new Deviation
            {
                TenantId = tenant.Id,
                ProjectId = project3.Id,
                Title = "Køyrde i autovern",
                Description = "Gravemaskin skadet autovern på sørsiden. Utbedret same dag.",
                Status = DeviationStatus.Closed,
                Severity = DeviationSeverity.Medium,
                ReportedById = bruker.Id,
                ClosedById = admin.Id,
                ClosedAt = DateTimeOffset.UtcNow.AddDays(-3)
            },
            new Deviation
            {
                TenantId = tenant.Id,
                ProjectId = project3.Id,
                Title = "Manglande skilting ved omkøyring",
                Description = "Skilting for omkøyring var ikkje sett opp før arbeid starta. Retta opp innan 30 min.",
                Status = DeviationStatus.Closed,
                Severity = DeviationSeverity.Low,
                ReportedById = admin.Id,
                ClosedById = admin.Id,
                ClosedAt = DateTimeOffset.UtcNow.AddDays(-7)
            }
        };

        db.Deviations.AddRange(deviations);

        // Help content — Field Worker tutorials
        var helpContents = new List<HelpContent>
        {
            new HelpContent
            {
                PageIdentifier = "page:hours/clock-in",
                Title = "Slik stempler du inn",
                Body = """
                ## Stempling

                1. Trykk **Stemple inn** på dashboardet
                2. Velg prosjekt eller oppdrag fra listen
                3. Velg kategori (Arbeid, Reise, osv.)
                4. Trykk **Start**
                5. Når du er ferdig, trykk **Stemple ut**

                Tiden beregnes automatisk. Du kan legge til pause i etterkant.
                """,
                RoleScope = "field-worker",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:deviations/new",
                Title = "Slik rapporterer du avvik",
                Body = """
                ## Rapportere avvik

                1. Trykk **Nytt avvik** fra avvikslisten eller dashboardet
                2. Fyll inn tittel og beskrivelse
                3. Velg alvorlighetsgrad (Lav, Middels, Høy, Kritisk)
                4. Ta bilde av avviket med kameraknappen
                5. Velg prosjekt avviket tilhører
                6. Trykk **Send inn**

                Avviket blir sendt til prosjektleder for behandling.
                """,
                RoleScope = "field-worker",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:jobs/new",
                Title = "Slik oppretter du oppdrag",
                Body = """
                ## Opprett oppdrag

                1. Trykk **Nytt oppdrag** fra oppdragslisten
                2. Gi oppdraget et navn
                3. Velg kunde (bedrift eller privatperson)
                4. Legg til adresse eller GPS-posisjon
                5. Trykk **Opprett**

                Oppdrag er ment for raske jobber som ikke krever full prosjektoppsett.
                """,
                RoleScope = "field-worker",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:checklists/complete",
                Title = "Slik fyller du ut sjekklister",
                Body = """
                ## Fylle ut sjekkliste

                1. Åpne sjekklisten fra prosjekt eller oppdrag
                2. Gå gjennom hvert punkt og trykk **OK** eller **Ikke relevant**
                3. Fyll inn eventuelle datafelt (tall, tekst, dato)
                4. Ta bilder der det kreves
                5. Trykk **Fullfør** og signer med din lagrede signatur

                Sjekklisten lagres automatisk underveis.
                """,
                RoleScope = "field-worker",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:hours/manual",
                Title = "Slik fører du timer manuelt",
                Body = """
                ## Manuell timeføring

                1. Gå til **Timer** og trykk **Legg til manuelt**
                2. Velg dato og prosjekt/oppdrag
                3. Skriv inn antall timer og velg kategori
                4. Legg til kommentar ved behov
                5. Trykk **Lagre**

                Manuelt førte timer må godkjennes av prosjektleder.
                """,
                RoleScope = "field-worker",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:profile",
                Title = "Slik oppdaterer du profilen din",
                Body = """
                ## Oppdater profil

                1. Trykk på profilikonet i toppmenyen
                2. Rediger navn, telefonnummer eller annen informasjon
                3. Last opp profilbilde om ønskelig
                4. Trykk **Lagre endringer**

                Profilen din er synlig for alle bedrifter du er tilknyttet.
                """,
                RoleScope = "field-worker",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:profile/certifications",
                Title = "Slik laster du opp sertifikater",
                Body = """
                ## Last opp sertifikat

                1. Gå til **Profil** og velg **Sertifikater**
                2. Trykk **Legg til sertifikat**
                3. Ta bilde eller last opp PDF av sertifikatet
                4. Utløpsdato hentes automatisk via OCR
                5. Kontroller at informasjonen stemmer, og trykk **Lagre**

                Du får varsel 90, 30 og 0 dager før utløp.
                """,
                RoleScope = "field-worker",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:chemicals",
                Title = "Slik bruker du stoffkartoteket",
                Body = """
                ## Stoffkartotek

                1. Gå til **Stoffkartotek** fra menyen
                2. Søk etter kjemikalie med navn eller strekkode
                3. Trykk på et stoff for å se sikkerhetsdatablad og faremerking
                4. Les AI-oppsummeringen for rask oversikt over fare og tiltak
                5. Bruk **Skann strekkode** for å finne stoff direkte

                Kartoteket er tilgjengelig offline med sist synkroniserte data.
                """,
                RoleScope = "field-worker",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:forefallende",
                Title = "Slik bruker du forefallende-oversikten",
                Body = """
                ## Forefallende oppgaver

                1. Gå til **Forefallende** fra dashboardet
                2. Se oversikt over kommende frister og oppgaver
                3. Trykk på en oppgave for å gå direkte til den
                4. Filtrer etter type (sjekklister, sertifikater, vedlikehold)

                Oversikten oppdateres automatisk basert på dine tilknyttede prosjekter.
                """,
                RoleScope = "field-worker",
                Language = "nb"
            },

            // Help content — Project Leader tutorials
            new HelpContent
            {
                PageIdentifier = "page:projects/create",
                Title = "Slik oppretter du prosjekter",
                Body = """
                ## Opprett prosjekt

                1. Gå til **Prosjekter** og trykk **Nytt prosjekt**
                2. Fyll inn prosjektnavn, kunde og adresse
                3. Sett startdato og eventuell sluttdato
                4. Legg til oppgavegrupper for å knytte sjekklister, utstyr og prosedyrer
                5. Trykk **Opprett prosjekt**

                Prosjektet blir synlig for alle teammedlemmer umiddelbart.
                """,
                RoleScope = "project-leader",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:admin/hours",
                Title = "Slik godkjenner du timer",
                Body = """
                ## Godkjenne timer

                1. Gå til **Timer** og velg **Til godkjenning**
                2. Se gjennom innsendte timer per ansatt
                3. Trykk **Godkjenn** eller **Avvis** med kommentar
                4. Godkjente timer kan eksporteres til lønnssystem

                Du kan filtrere etter uke, prosjekt eller ansatt.
                """,
                RoleScope = "project-leader",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:deviations/manage",
                Title = "Slik behandler du avvik",
                Body = """
                ## Behandle avvik

                1. Gå til **Avvik** og se listen over åpne avvik
                2. Trykk på et avvik for å se detaljer og bilder
                3. Tildel avviket til en ansatt om nødvendig
                4. Sett status til **Under behandling** og legg til tiltak
                5. Når tiltaket er gjennomført, sett status til **Lukket**

                Alle statusendringer loggføres automatisk.
                """,
                RoleScope = "project-leader",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:checklists/templates",
                Title = "Slik oppretter du sjekklistemaler",
                Body = """
                ## Opprett sjekklistemal

                1. Gå til **Maler** og trykk **Ny mal**
                2. Velg type: sjekkliste, skjema eller prosedyre
                3. Legg til punkter med OK/Ikke relevant-knapper
                4. Legg til datafelt (tekst, tall, dato, bilde) ved behov
                5. Lagre malen — den kan gjenbrukes på tvers av prosjekter

                Maler kan versjoneres. Eksisterende utfyllinger beholder sin versjon.
                """,
                RoleScope = "project-leader",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:hms/sja",
                Title = "Slik oppretter du SJA",
                Body = """
                ## Sikker jobb-analyse (SJA)

                1. Gå til **HMS** og trykk **Ny SJA**
                2. Beskriv arbeidsoppgaven og stedet
                3. Identifiser farer og legg til risikovurdering
                4. Definer tiltak for hver fare
                5. Alle deltakere signerer automatisk — disputer kan registreres

                SJA-en arkiveres med full historikk og signaturer.
                """,
                RoleScope = "project-leader",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:employees/manage",
                Title = "Slik administrerer du ansatte",
                Body = """
                ## Administrere ansatte

                1. Gå til **Ansatte** fra menyen
                2. Se oversikt over alle ansatte og deres sertifikatstatus
                3. Trykk på en ansatt for å se profil og CV
                4. Administrer roller og tilganger per ansatt
                5. Se varsler for utgående sertifikater

                Ansatte med utgåtte sertifikater markeres med varseltrekant.
                """,
                RoleScope = "project-leader",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:calendar/events",
                Title = "Slik bruker du kalenderen",
                Body = """
                ## Kalender

                1. Gå til **Kalender** fra menyen
                2. Se oversikt over planlagte aktiviteter (vernerunder, HMS-møter, frister)
                3. Trykk på en dato for å se detaljer
                4. Opprett nye hendelser med **Ny hendelse**-knappen
                5. Filtrer etter type aktivitet

                Kalenderen synkroniserer med prosjektfrister og sertifikatutløp.
                """,
                RoleScope = "project-leader",
                Language = "nb"
            },

            // Help content — Admin tutorials
            new HelpContent
            {
                PageIdentifier = "page:admin/settings/company",
                Title = "Slik konfigurerer du bedriftsprofilen",
                Body = """
                ## Bedriftsprofil

                1. Gå til **Innstillinger** > **Bedriftsprofil**
                2. Last opp bedriftslogo (fargetema utledes automatisk)
                3. Fyll inn kontaktinformasjon og adresse
                4. Konfigurer standard tidssone
                5. Trykk **Lagre**

                Logoen vises i alle eksporterte dokumenter og PDF-rapporter.
                """,
                RoleScope = "tenant-admin",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:admin/settings/roles",
                Title = "Slik administrerer du roller",
                Body = """
                ## Roller og tilganger

                1. Gå til **Innstillinger** > **Roller**
                2. Se standardrollene: Admin, Prosjektleder, Feltarbeider
                3. Tilpass hvilke tilganger prosjektledere har
                4. Tildel roller til ansatte via ansattlisten

                Rollene styrer hva brukerne ser og kan gjøre i systemet.
                """,
                RoleScope = "tenant-admin",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:admin/settings/gps",
                Title = "Slik aktiverer du GPS",
                Body = """
                ## GPS-innstillinger

                1. Gå til **Innstillinger** > **GPS**
                2. Aktiver GPS-sporing for bedriften
                3. Hver ansatt vil bli bedt om å godta eller avslå GPS
                4. GPS registreres kun ved handlinger (innsjekking, stempling, avvik)

                Ansatte som avslår GPS kan fortsatt bruke alle funksjoner.
                """,
                RoleScope = "tenant-admin",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:admin/settings/categories",
                Title = "Slik oppretter du avvikskategorier",
                Body = """
                ## Avvikskategorier

                1. Gå til **Innstillinger** > **Avvikskategorier**
                2. Trykk **Ny kategori**
                3. Gi kategorien et navn (f.eks. HMS, Kvalitet, Miljø)
                4. Kategoriene blir tilgjengelige ved opprettelse av avvik

                Kategorier hjelper med filtrering og rapportering.
                """,
                RoleScope = "tenant-admin",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:admin/settings/schedules",
                Title = "Slik oppretter du turnusordninger",
                Body = """
                ## Turnusordninger

                1. Gå til **Innstillinger** > **Turnus**
                2. Trykk **Ny turnusordning**
                3. Definer arbeidsdager, hviledager og rotasjon
                4. Knytt turnusordningen til ansatte

                Turnusordningen brukes ved beregning av overtid og tillegg.
                """,
                RoleScope = "tenant-admin",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:admin/settings/allowances",
                Title = "Slik konfigurerer du tillegg",
                Body = """
                ## Tillegg og satser

                1. Gå til **Innstillinger** > **Tillegg**
                2. Opprett tilleggstyper (diett, bompenger, materiell, osv.)
                3. Sett faste satser eller marker som beløpsfelt
                4. Tilleggene blir tilgjengelige ved timeføring

                Tillegg eksporteres sammen med timer til lønnssystemet.
                """,
                RoleScope = "tenant-admin",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:admin/settings/notifications",
                Title = "Slik konfigurerer du varsler",
                Body = """
                ## Varslingsinnstillinger

                1. Gå til **Innstillinger** > **Varsler**
                2. Velg hvilke hendelser som skal utløse varsler
                3. Aktiver e-postvarsler per hendelsestype
                4. Konfigurer mottakere for kritiske varsler

                Alle brukere får varsler i appen. E-post er valgfritt.
                """,
                RoleScope = "tenant-admin",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:export",
                Title = "Slik eksporterer du dokumentasjon",
                Body = """
                ## Eksport

                1. Gå til **Eksport** fra menyen
                2. Velg hva du vil eksportere (prosjekt, sjekklister, avvik, timer)
                3. Velg format: PDF eller Excel
                4. Filtrer etter periode og prosjekt
                5. Trykk **Last ned**

                PDF-rapporter inkluderer logo, signaturer og bilder.
                """,
                RoleScope = "tenant-admin",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:admin/settings/subscription",
                Title = "Slik administrerer du abonnement",
                Body = """
                ## Abonnement

                1. Gå til **Innstillinger** > **Abonnement**
                2. Se gjeldende plan og antall brukere
                3. Oppgrader eller nedjuster etter behov
                4. Administrer fakturainformasjon

                Ved kansellering beholdes data i 90 dager.
                """,
                RoleScope = "tenant-admin",
                Language = "nb"
            },
            new HelpContent
            {
                PageIdentifier = "page:admin/onboarding",
                Title = "Slik tar du i bruk Solodoc",
                Body = """
                ## Kom i gang med Solodoc

                1. Fyll inn bedriftsprofil og last opp logo
                2. Inviter ansatte via e-post
                3. Opprett ditt første prosjekt
                4. Legg til sjekklistemaler fra basemaler eller lag egne
                5. Aktiver GPS og varsler etter behov
                6. Ansatte laster ned appen og logger inn

                Kontakt support hvis du trenger hjelp med oppsett.
                """,
                RoleScope = "tenant-admin",
                Language = "nb"
            }
        };

        db.HelpContents.AddRange(helpContents);

        // ── Default deviation categories (Norwegian HMS standards) ──
        var defaultCategories = new[]
        {
            ("Personskade", 1),
            ("Nestenulykke", 2),
            ("Materiell skade", 3),
            ("Farlig tilstand", 4),
            ("Kvalitetsavvik", 5),
            ("Miljøavvik", 6),
            ("Sikkerhet", 7),
            ("Brann/eksplosjon", 8),
            ("Kjemikalie/gass", 9),
            ("Ergonomi", 10),
            ("Annet", 99)
        };
        foreach (var tid in new[] { tenant.Id, tenant2.Id })
        {
            foreach (var (name, sort) in defaultCategories)
            {
                db.DeviationCategories.Add(new DeviationCategory
                {
                    TenantId = tid,
                    Name = name,
                    SortOrder = sort,
                    IsDefault = true,
                    IsActive = true
                });
            }
        }

        // ── Default equipment type categories ──
        var defaultEquipTypes = new[]
        {
            ("Bil", 1), ("Lastebil", 2), ("Varebil", 3), ("Traktor", 4),
            ("Gravemaskin", 5), ("Hjullaster", 6), ("Lift", 7), ("Stillas", 8),
            ("Håndverktøy", 9), ("Elektroverktøy", 10), ("Måleinstrument", 11),
            ("Verneutstyr", 12), ("Tilhenger", 13), ("Generator", 14), ("Annet", 99)
        };
        foreach (var tid in new[] { tenant.Id, tenant2.Id })
        {
            foreach (var (name, sort) in defaultEquipTypes)
            {
                db.EquipmentTypeCategories.Add(new EquipmentTypeCategory
                {
                    TenantId = tid,
                    Name = name,
                    SortOrder = sort,
                    IsDefault = true,
                    IsActive = true
                });
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seeded: 2 tenants, 2 users, 3 projects, 5 deviations, {HelpCount} help content entries, {CatCount} deviation categories, {EquipCount} equipment types",
            helpContents.Count, defaultCategories.Length * 2, defaultEquipTypes.Length * 2);
    }
}
