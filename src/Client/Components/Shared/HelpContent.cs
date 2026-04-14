using MudBlazor;

namespace Solodoc.Client.Components.Shared;

public class HelpSlide
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Icon { get; set; }
    public List<string> Tips { get; set; } = [];
    public string? Shortcut { get; set; }
}

public static class HelpContent
{
    public static List<HelpSlide> GetSlides(string pageKey) => pageKey switch
    {
        "dashboard" => [
            new() { Title = "Dashboardet ditt", Description = "Her ser du en oversikt over alt — aktive prosjekter, timer denne måneden, åpne avvik og hvem som er på jobb.", Icon = Icons.Material.Filled.Dashboard,
                Tips = ["Trykk på tallene for å gå direkte til den modulen", "Forefallende viser frister og oppgaver som trenger oppmerksomhet"] },
            new() { Title = "Stemple inn og ut", Description = "Bruk knappene overst for å starte og stoppe arbeidsdagen. Timene registreres automatisk.", Icon = Icons.Material.Filled.PlayArrow,
                Tips = ["Velg prosjekt eller oppdrag når du stempler inn", "Stemple ut-knappen viser hvor lenge du har jobbet"] },
            new() { Title = "Sjekk inn på byggeplass", Description = "Sjekk inn betyr at du er fysisk tilstede. Stemple inn betyr at du jobber. Begge er separate.", Icon = Icons.Material.Filled.Login,
                Tips = ["Sjekk inn via QR-kode eller manuelt fra dashboardet", "Pa jobb na-widgeten viser hvem som er hvor"] },
        ],
        "projects" => [
            new() { Title = "Prosjekter", Description = "Store oppdrag med full livssyklus — planlegging, gjennomforing, dokumentasjon og avslutning.", Icon = Icons.Material.Filled.Folder,
                Tips = ["Klikk på et prosjekt for å se detaljer, sjekklister og avvik", "Bruk sok og sortering for å finne riktig prosjekt"] },
            new() { Title = "Prosjektstatus", Description = "Hvert prosjekt har en status: Planlagt, Aktiv, Fullført eller Kansellert.", Icon = Icons.Material.Filled.Flag,
                Tips = ["Endre status med knappene på prosjektdetaljsiden", "Aktive prosjekter vises på dashboardet"] },
            new() { Title = "Sjekklister i prosjektet", Description = "Tilordne sjekklister til prosjektet. Batch-opprett for hele blokka med nummerering.", Icon = Icons.Material.Filled.Checklist,
                Tips = ["Trykk Opprett sjekkliste inne på prosjektet", "Velg mal og antall — navngivning genereres automatisk"] },
        ],
        "jobs" => [
            new() { Title = "Oppdrag", Description = "Raske jobber på under 2 minutter — servicebesøk, reparasjoner, kundebesøk.", Icon = Icons.Material.Filled.Build,
                Tips = ["Opprett med beskrivelse, kunde og adresse", "Legg til deleliste med materialer og antall", "Koble til bedriftskunde eller privatperson"] },
        ],
        "hours" => [
            new() { Title = "Timelister", Description = "Registrer timer med start- og sluttid. Se uken din som kalenderblokker.", Icon = Icons.Material.Filled.Schedule,
                Tips = ["Klikk på en dag for å registrere timer", "Fyll inn fra-til, prosjekt og kategori", "Blokker vises i dagkolonner med fargekoding"] },
            new() { Title = "Overtid og fravær", Description = "Overtid beregnes automatisk basert på regler. Registrer fravær som ferie, sykdom eller avspasering.", Icon = Icons.Material.Filled.AccessTime,
                Tips = ["Overtid utover normalarbeidstid markeres gult", "Velg om overtid skal utbetales eller gå i timebanken"] },
        ],
        "hours-admin" => [
            new() { Title = "Timer-oversikt", Description = "Se alle ansattes timer i et heatmap. Godkjenn timer dag for dag.", Icon = Icons.Material.Filled.QueryStats,
                Tips = ["Grønn = godkjent, gul = registrert, rod = mangler", "Bla = fravær (ferie, sykdom, avspasering)"] },
            new() { Title = "Hurtiggodkjenning", Description = "Naviger med piltaster i heatmapet. Trykk Enter for å godkjenne.", Icon = Icons.Material.Filled.Keyboard,
                Tips = ["Forste Enter: viser Godkjenn?", "Andre Enter: godkjenner dagen", "Escape: lukker detaljpanelet"],
                Shortcut = "Piltaster: naviger | Enter + Enter: godkjenn | Escape: lukk" },
            new() { Title = "Eksport", Description = "Eksporter månedens timer som CSV for lonnssystem.", Icon = Icons.Material.Filled.FileDownload,
                Tips = ["Filtrer på ansatt for å se én person", "CSV-filen inneholder alle detaljer for lønnskjøring"] },
        ],
        "deviations" => [
            new() { Title = "Avvik", Description = "Rapporter, folg opp og lukk avvik. Tre statuser: Åpen (rod), Under behandling (gul), Lukket (grønn).", Icon = Icons.Material.Filled.ReportProblem,
                Tips = ["Rapporter med tittel, beskrivelse, alvorlighet og bilde", "Koble til prosjekt, oppdrag eller lokasjon", "Tildel ansvårlig og sett frist for korrigerende tiltak"] },
        ],
        "checklists" => [
            new() { Title = "Sjekklister og maler", Description = "Lag maler én gang, bruk dem overalt. Fyll ut med OK/Irrelevant, tekst, tall, dato, bilder og signatur.", Icon = Icons.Material.Filled.Checklist,
                Tips = ["Malbiblioteket viser alle maler som store kort", "Klikk på en mal for forhåndsvisning", "Tilordne til prosjekt med batch-navngivning"] },
            new() { Title = "Malbygger", Description = "Bygg maler visuelt — legg til elementer med ett klikk. Hvert element viser forhåndsvisning av hvordan det ser ut.", Icon = Icons.Material.Filled.Construction,
                Tips = ["7 elementtyper: Sjekkpunkt, Tekst, Tall, Dato, Nedtrekk, Foto, Signatur", "Dra elementer opp/ned for å endre rekkefolge", "Dupliser maler med ett klikk"] },
            new() { Title = "Importere maler", Description = "Har du maler fra for? Last opp PDF, Excel eller Word — vi leser og konverterer automatisk.", Icon = Icons.Material.Filled.UploadFile,
                Tips = ["AI leser dokumentet og trekker ut sjekkpunktene", "Sjekk og juster i malbyggeren etterpå"] },
        ],
        "hms" => [
            new() { Title = "HMS", Description = "Sikker jobb-analyse (SJA), vernerunder og HMS-moter — alt samlet.", Icon = Icons.Material.Filled.HealthAndSafety,
                Tips = ["Opprett SJA for hvert nytt arbeidsomrade", "Legg til farer, tiltak og deltakere", "HMS-moter med aksjonsplan og referat"] },
        ],
        "locations" => [
            new() { Title = "Lokasjoner", Description = "Faste steder som kontor, lager og verksted. Koble sjekklister og avvik til lokasjoner.", Icon = Icons.Material.Filled.LocationOn,
                Tips = ["Opprett med navn, type, adresse og beskrivelse", "Kart vises automatisk basert på adresse", "Tilordne sjekklistemaler til lokasjon"] },
        ],
        "equipment" => [
            new() { Title = "Maskinpark", Description = "Register over alt utstyr med vedlikeholdslogg og dokumentasjon.", Icon = Icons.Material.Filled.PrecisionManufacturing,
                Tips = ["Registrer type, serienummer, årgang", "Logg vedlikehold med dato og beskrivelse", "Koble maskiner til prosjekter"] },
        ],
        "chemicals" => [
            new() { Title = "Kjemikalieregister", Description = "Sikkerhetsdatablad, GHS-piktogrammer og verneutstyr på ett sted.", Icon = Icons.Material.Filled.Science,
                Tips = ["Sok i database eller registrer manuelt", "AI oppsummerer sikkerhetsdatablad på norsk", "Skann strekkode for hurtigoppslag"] },
        ],
        "calendar" => [
            new() { Title = "Kalender", Description = "Se alle hendelser, skift og frister i maned- eller ukevisning.", Icon = Icons.Material.Filled.CalendarMonth,
                Tips = ["Bytt mellom maned og uke med knappene", "Klikk bedrifts-ikonet for å se alle bedrifter samtidig", "Opprett hendelser direkte i kalenderen"] },
        ],
        "employees" => [
            new() { Title = "Ansatte", Description = "Administrer teamet — inviter nye, endre roller, se sertifikater.", Icon = Icons.Material.Filled.People,
                Tips = ["Inviter med e-post — de får en lenke", "Endre rolle via redigeringsknappen", "Sertifikater folger personen på tvers av bedrifter"] },
        ],
        "contacts" => [
            new() { Title = "Kontakter", Description = "Kunder, leverandorer og underentreprenorer.", Icon = Icons.Material.Filled.Contacts,
                Tips = ["Registrer bedriftskunder med org.nummer", "Registrer privatpersoner med adresse", "Koble kontakter til prosjekter og oppdrag"] },
        ],
        "reports" => [
            new() { Title = "Rapporter", Description = "Eksporter timer, avvik og prosjektdata som PDF eller Excel.", Icon = Icons.Material.Filled.Assessment,
                Tips = ["Filtrer på periode, prosjekt og ansatt", "Klar for Arbeidstilsynet, revisor eller kunde"] },
        ],
        _ => []
    };
}
