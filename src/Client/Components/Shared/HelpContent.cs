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
            new() { Title = "Dashboardet ditt", Description = "Her ser du en oversikt over alt — aktive prosjekter, timer denne maneden, apne avvik og hvem som er pa jobb.", Icon = Icons.Material.Filled.Dashboard,
                Tips = ["Trykk pa tallene for a ga direkte til den modulen", "Forefallende viser frister og oppgaver som trenger oppmerksomhet"] },
            new() { Title = "Stemple inn og ut", Description = "Bruk knappene overst for a starte og stoppe arbeidsdagen. Timene registreres automatisk.", Icon = Icons.Material.Filled.PlayArrow,
                Tips = ["Velg prosjekt eller oppdrag nar du stempler inn", "Stemple ut-knappen viser hvor lenge du har jobbet"] },
            new() { Title = "Sjekk inn pa byggeplass", Description = "Sjekk inn betyr at du er fysisk tilstede. Stemple inn betyr at du jobber. Begge er separate.", Icon = Icons.Material.Filled.Login,
                Tips = ["Sjekk inn via QR-kode eller manuelt fra dashboardet", "Pa jobb na-widgeten viser hvem som er hvor"] },
        ],
        "projects" => [
            new() { Title = "Prosjekter", Description = "Store oppdrag med full livssyklus — planlegging, gjennomforing, dokumentasjon og avslutning.", Icon = Icons.Material.Filled.Folder,
                Tips = ["Klikk pa et prosjekt for a se detaljer, sjekklister og avvik", "Bruk sok og sortering for a finne riktig prosjekt"] },
            new() { Title = "Prosjektstatus", Description = "Hvert prosjekt har en status: Planlagt, Aktiv, Fullfort eller Kansellert.", Icon = Icons.Material.Filled.Flag,
                Tips = ["Endre status med knappene pa prosjektdetaljsiden", "Aktive prosjekter vises pa dashboardet"] },
            new() { Title = "Sjekklister i prosjektet", Description = "Tilordne sjekklister til prosjektet. Batch-opprett for hele blokka med nummerering.", Icon = Icons.Material.Filled.Checklist,
                Tips = ["Trykk Opprett sjekkliste inne pa prosjektet", "Velg mal og antall — navngivning genereres automatisk"] },
        ],
        "jobs" => [
            new() { Title = "Oppdrag", Description = "Raske jobber pa under 2 minutter — servicebesok, reparasjoner, kundebesok.", Icon = Icons.Material.Filled.Build,
                Tips = ["Opprett med beskrivelse, kunde og adresse", "Legg til deleliste med materialer og antall", "Koble til bedriftskunde eller privatperson"] },
        ],
        "hours" => [
            new() { Title = "Timelister", Description = "Registrer timer med start- og sluttid. Se uken din som kalenderblokker.", Icon = Icons.Material.Filled.Schedule,
                Tips = ["Klikk pa en dag for a registrere timer", "Fyll inn fra-til, prosjekt og kategori", "Blokker vises i dagkolonner med fargekoding"] },
            new() { Title = "Overtid og fravær", Description = "Overtid beregnes automatisk basert pa regler. Registrer fravær som ferie, sykdom eller avspasering.", Icon = Icons.Material.Filled.AccessTime,
                Tips = ["Overtid utover normalarbeidstid markeres gult", "Velg om overtid skal utbetales eller ga i timebanken"] },
        ],
        "hours-admin" => [
            new() { Title = "Timer-oversikt", Description = "Se alle ansattes timer i et heatmap. Godkjenn timer dag for dag.", Icon = Icons.Material.Filled.QueryStats,
                Tips = ["Gronn = godkjent, gul = registrert, rod = mangler", "Bla = fravaer (ferie, sykdom, avspasering)"] },
            new() { Title = "Hurtiggodkjenning", Description = "Naviger med piltaster i heatmapet. Trykk Enter for a godkjenne.", Icon = Icons.Material.Filled.Keyboard,
                Tips = ["Forste Enter: viser Godkjenn?", "Andre Enter: godkjenner dagen", "Escape: lukker detaljpanelet"],
                Shortcut = "Piltaster: naviger | Enter + Enter: godkjenn | Escape: lukk" },
            new() { Title = "Eksport", Description = "Eksporter manedens timer som CSV for lonnssystem.", Icon = Icons.Material.Filled.FileDownload,
                Tips = ["Filtrer pa ansatt for a se én person", "CSV-filen inneholder alle detaljer for lonnskjoring"] },
        ],
        "deviations" => [
            new() { Title = "Avvik", Description = "Rapporter, folg opp og lukk avvik. Tre statuser: Apen (rod), Under behandling (gul), Lukket (gronn).", Icon = Icons.Material.Filled.ReportProblem,
                Tips = ["Rapporter med tittel, beskrivelse, alvorlighet og bilde", "Koble til prosjekt, oppdrag eller lokasjon", "Tildel ansvarlig og sett frist for korrigerende tiltak"] },
        ],
        "checklists" => [
            new() { Title = "Sjekklister og maler", Description = "Lag maler én gang, bruk dem overalt. Fyll ut med OK/Irrelevant, tekst, tall, dato, bilder og signatur.", Icon = Icons.Material.Filled.Checklist,
                Tips = ["Malbiblioteket viser alle maler som store kort", "Klikk pa en mal for forhandsvisning", "Tilordne til prosjekt med batch-navngivning"] },
            new() { Title = "Malbygger", Description = "Bygg maler visuelt — legg til elementer med ett klikk. Hvert element viser forhåndsvisning av hvordan det ser ut.", Icon = Icons.Material.Filled.Construction,
                Tips = ["7 elementtyper: Sjekkpunkt, Tekst, Tall, Dato, Nedtrekk, Foto, Signatur", "Dra elementer opp/ned for a endre rekkefolge", "Dupliser maler med ett klikk"] },
            new() { Title = "Importere maler", Description = "Har du maler fra for? Last opp PDF, Excel eller Word — vi leser og konverterer automatisk.", Icon = Icons.Material.Filled.UploadFile,
                Tips = ["AI leser dokumentet og trekker ut sjekkpunktene", "Sjekk og juster i malbyggeren etterpå"] },
        ],
        "hms" => [
            new() { Title = "HMS", Description = "Sikker jobb-analyse (SJA), vernerunder og HMS-moter — alt samlet.", Icon = Icons.Material.Filled.HealthAndSafety,
                Tips = ["Opprett SJA for hvert nytt arbeidsomrade", "Legg til farer, tiltak og deltakere", "HMS-moter med aksjonsplan og referat"] },
        ],
        "locations" => [
            new() { Title = "Lokasjoner", Description = "Faste steder som kontor, lager og verksted. Koble sjekklister og avvik til lokasjoner.", Icon = Icons.Material.Filled.LocationOn,
                Tips = ["Opprett med navn, type, adresse og beskrivelse", "Kart vises automatisk basert pa adresse", "Tilordne sjekklistemaler til lokasjon"] },
        ],
        "equipment" => [
            new() { Title = "Maskinpark", Description = "Register over alt utstyr med vedlikeholdslogg og dokumentasjon.", Icon = Icons.Material.Filled.PrecisionManufacturing,
                Tips = ["Registrer type, serienummer, argang", "Logg vedlikehold med dato og beskrivelse", "Koble maskiner til prosjekter"] },
        ],
        "chemicals" => [
            new() { Title = "Kjemikalieregister", Description = "Sikkerhetsdatablad, GHS-piktogrammer og verneutstyr pa ett sted.", Icon = Icons.Material.Filled.Science,
                Tips = ["Sok i database eller registrer manuelt", "AI oppsummerer sikkerhetsdatablad pa norsk", "Skann strekkode for hurtigoppslag"] },
        ],
        "calendar" => [
            new() { Title = "Kalender", Description = "Se alle hendelser, skift og frister i maned- eller ukevisning.", Icon = Icons.Material.Filled.CalendarMonth,
                Tips = ["Bytt mellom maned og uke med knappene", "Klikk bedrifts-ikonet for a se alle bedrifter samtidig", "Opprett hendelser direkte i kalenderen"] },
        ],
        "employees" => [
            new() { Title = "Ansatte", Description = "Administrer teamet — inviter nye, endre roller, se sertifikater.", Icon = Icons.Material.Filled.People,
                Tips = ["Inviter med e-post — de far en lenke", "Endre rolle via redigeringsknappen", "Sertifikater folger personen pa tvers av bedrifter"] },
        ],
        "contacts" => [
            new() { Title = "Kontakter", Description = "Kunder, leverandorer og underentreprenorer.", Icon = Icons.Material.Filled.Contacts,
                Tips = ["Registrer bedriftskunder med org.nummer", "Registrer privatpersoner med adresse", "Koble kontakter til prosjekter og oppdrag"] },
        ],
        "reports" => [
            new() { Title = "Rapporter", Description = "Eksporter timer, avvik og prosjektdata som PDF eller Excel.", Icon = Icons.Material.Filled.Assessment,
                Tips = ["Filtrer pa periode, prosjekt og ansatt", "Klar for Arbeidstilsynet, revisor eller kunde"] },
        ],
        _ => []
    };
}
