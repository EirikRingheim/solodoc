using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Infrastructure.Persistence;

namespace Solodoc.Api.Endpoints;

public static class ChatbotEndpoints
{
    public static WebApplication MapChatbotEndpoints(this WebApplication app)
    {
        app.MapPost("/api/chatbot", Chat).RequireAuthorization();
        return app;
    }

    private static readonly string SystemPrompt = """
        Du er Solodoc-assistenten — en hjelpsom AI som hjelper brukere med kvalitets- og prosjektstyringssystemet Solodoc.
        Svar alltid på norsk. Vær kort og presis. Bruk du-form.

        Solodoc er et system for bygg, anlegg, handverkere og landbruk med disse modulene:

        DASHBOARD (/)
        - Oversikt over prosjekter, timer, avvik, forefallende
        - Stemple inn/ut-knapp for timeregistrering
        - Sjekk inn/ut for byggeplassregistrering

        PROSJEKTER (/projects)
        - Opprett, rediger, endre status (Planlagt → Aktiv → Fullført)
        - Sjekklister, avvik og mannskap per prosjekt

        OPPDRAG (/jobs)
        - Raske jobber: servicebesøk, reparasjoner
        - Deleliste med materialer

        TIMEFØRING (/hours)
        - Registrer timer med fra-til og prosjekt
        - Kalendervisning med dagblokker
        - Admin: heatmap-oversikt, godkjenn med Enter+Enter
        - Piltaster for navigering i heatmap

        AVVIK (/deviations)
        - Rapporter med tittel, beskrivelse, alvorlighet, foto
        - Tre statuser: Åpen → Under behandling → Lukket
        - Koble til prosjekt, oppdrag eller lokasjon

        SJEKKLISTER (/checklists)
        - Malbibliotek med forhåndsvisning
        - Elementtyper: Sjekkpunkt (OK/Irrelevant), Tekst, Tall, Dato, Nedtrekk, Foto, Signatur
        - Batch-oppretting for prosjekter (f.eks. "Leilighet 1-120")
        - Importer maler fra PDF/Excel/Word

        HMS (/hms)
        - SJA (Sikker jobb-analyse) med farer og tiltak
        - HMS-møter med aksjonsplan
        - Vernerunder

        LOKASJONER (/locations)
        - Kontor, lager, verksted med kart
        - Tilordne sjekklister og se avvik per lokasjon

        MASKINPARK (/machines)
        - Utstyrsregister med vedlikeholdslogg

        KJEMIKALIER (/chemicals)
        - Sikkerhetsdatablad, GHS-piktogrammer

        KONTAKTER (/contacts)
        - Kunder (bedrift/privat), leverandører

        KALENDER (/calendar)
        - Måned/uke-visning, vis alle bedrifter samtidig

        ANSATTE (/employees)
        - Inviter, endre roller, se sertifikater
        - Sertifikater følger personen på tvers av bedrifter

        ADMIN
        - Roller (/admin/roles): Opprett egne roller med tilgangstyring
        - Skift (/admin/shifts): Turnus, overtidsregler
        - Innstillinger (/admin/settings): Bedriftsprofil, GPS

        OFFLINE
        - Appen fungerer uten nett
        - Data synkroniseres automatisk når tilbake online

        Svar med konkrete steg brukeren kan følge. Bruk menynavn og knappnavn fra appen.
        Hvis du ikke vet svaret, si det ærlig.
        """;

    private static async Task<IResult> Chat(
        ChatRequest request,
        IConfiguration config,
        CancellationToken ct)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return Results.BadRequest(new { error = "AI-chatbot er ikke konfigurert." });

        if (string.IsNullOrWhiteSpace(request.Message))
            return Results.BadRequest(new { error = "Tom melding." });

        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var messages = new List<object>();

            // Add conversation history if provided
            if (request.History is not null)
            {
                foreach (var msg in request.History.TakeLast(10))
                {
                    messages.Add(new { role = msg.Role, content = msg.Content });
                }
            }

            // Add current message
            messages.Add(new { role = "user", content = request.Message });

            var body = new
            {
                model = "claude-haiku-4-5-20251001",
                max_tokens = 1024,
                system = SystemPrompt,
                messages
            };

            var response = await http.PostAsJsonAsync("https://api.anthropic.com/v1/messages", body, ct);
            if (!response.IsSuccessStatusCode)
                return Results.StatusCode(502);

            var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var text = result.GetProperty("content")[0].GetProperty("text").GetString() ?? "";

            return Results.Ok(new ChatResponse(text));
        }
        catch
        {
            return Results.StatusCode(502);
        }
    }
}

public record ChatRequest(string Message, List<ChatMessage>? History);
public record ChatMessage(string Role, string Content);
public record ChatResponse(string Reply);
