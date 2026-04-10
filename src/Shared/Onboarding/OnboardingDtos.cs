namespace Solodoc.Shared.Onboarding;

public record OnboardingStatusDto(
    bool IsCompleted,
    string? IndustryType,
    string? CompanySize,
    List<string> EnabledModules,
    string SubscriptionTier,
    DateTimeOffset? TrialEndsAt);

public record CreateTenantOnboardingRequest(
    string CompanyName,
    string? OrgNumber,
    string IndustryType,
    string CompanySize);

public record SaveOnboardingStep1Request(
    string CompanyName,
    string IndustryType,
    string CompanySize);

public record SaveOnboardingStep2Request(
    List<string> EnabledModules);

public record SaveOnboardingStep3Request(
    List<InviteEntry> Invites);

public record InviteEntry(string Email, string Role);

public record CompleteOnboardingRequest(bool Completed = true);

// ── Pricing ──────────────────────────────────────────
public static class PricingConfig
{
    public const int BasePriceMonthly = 990;        // kr/mnd for 1-5 users
    public const int BaseIncludedUsers = 5;
    public const int ExtraUserPrice = 150;           // kr/mnd per extra user
    public const int SubcontractorPrice = 49;        // kr/mnd per subcontractor
    public const decimal YearlyDiscount = 0.10m;     // 10% off
    public const int TrialDays = 30;

    // Template pricing
    public const int SingleTemplatePrice = 49;       // kr per template
    public const int Pack10Price = 299;              // kr for 10
    public const int Pack20Price = 499;              // kr for 20
    public const int Pack50Price = 999;              // kr for 50+

    public static int CalculateMonthly(int adminCount, int workerCount, int subcontractorCount)
    {
        var totalRegular = adminCount + workerCount;
        var extraUsers = Math.Max(0, totalRegular - BaseIncludedUsers);
        return BasePriceMonthly + (extraUsers * ExtraUserPrice) + (subcontractorCount * SubcontractorPrice);
    }

    public static int CalculateYearly(int monthly)
        => (int)(monthly * 12 * (1 - YearlyDiscount));
}

public static class IndustryTypes
{
    public static readonly List<IndustryOption> All =
    [
        new("bygg", "Bygg og anlegg", "Byggefirma, totalentreprenor, underentreprenor"),
        new("elektro", "Elektro", "Elektriker, elektroentreprenor"),
        new("rørlegger", "Rørlegger", "VVS, rørlegger, blikkenslager"),
        new("snekker", "Snekker og tømrer", "Snekker, tømrer, bygningsarbeider"),
        new("landbruk", "Landbruk", "Gard, skogbruk, gartneri"),
        new("maskin", "Maskin og transport", "Maskinentreprenor, transport, anlegg"),
        new("renhold", "Renhold og vedlikehold", "Renhold, vaktmester, facility management"),
        new("annet", "Annet", "Andre bransjer med fysisk feltarbeid"),
    ];
}

public record IndustryOption(string Key, string Label, string Description);

public static class ModuleDefinitions
{
    public static readonly List<ModuleOption> All =
    [
        new("projects", "Prosjekter", "Store prosjekter med full livssyklus", Icons.Folder),
        new("jobs", "Oppdrag", "Raske jobber, servicebesok, reparasjoner", Icons.Wrench),
        new("hours", "Timeføring", "Stemple inn/ut, timelister, overtid", Icons.Clock),
        new("deviations", "Avvik", "Rapporter og folg opp avvik", Icons.Warning),
        new("checklists", "Sjekklister", "Maler, utfylling, godkjenning", Icons.Checklist),
        new("hms", "HMS", "SJA, vernerunder, hendelsesrapporter", Icons.Shield),
        new("checkin", "Innsjekking", "QR-kode pa byggeplass, mannskapsliste", Icons.QrCode),
        new("chemicals", "Kjemikalieregister", "Sikkerhetsdatablad, GHS-piktogrammer", Icons.Flask),
        new("equipment", "Maskinpark", "Utstyr, vedlikehold, dokumentasjon", Icons.Truck),
        new("contacts", "Kontakter", "Kunder, leverandorer, underentreprenorer", Icons.People),
        new("locations", "Lokasjoner", "Kontor, lager, verksted", Icons.Pin),
    ];

    /// <summary>
    /// Returns recommended modules for an industry.
    /// </summary>
    public static List<string> GetRecommended(string industry) => industry switch
    {
        "bygg" => ["projects", "hours", "checklists", "deviations", "hms", "checkin", "contacts"],
        "elektro" => ["jobs", "hours", "checklists", "deviations", "chemicals", "contacts"],
        "rørlegger" => ["jobs", "hours", "checklists", "deviations", "chemicals", "contacts"],
        "snekker" => ["projects", "jobs", "hours", "checklists", "deviations", "contacts"],
        "landbruk" => ["equipment", "chemicals", "hms", "checklists", "hours"],
        "maskin" => ["projects", "equipment", "hours", "hms", "checklists"],
        "renhold" => ["jobs", "hours", "checklists", "locations", "contacts"],
        _ => ["projects", "jobs", "hours", "deviations", "checklists"],
    };

    private static class Icons
    {
        public const string Folder = "folder";
        public const string Wrench = "build";
        public const string Clock = "schedule";
        public const string Warning = "report_problem";
        public const string Checklist = "checklist";
        public const string Shield = "health_and_safety";
        public const string QrCode = "qr_code_2";
        public const string Flask = "science";
        public const string Truck = "local_shipping";
        public const string People = "contacts";
        public const string Pin = "location_on";
    }
}

public record ModuleOption(string Key, string Label, string Description, string Icon);
