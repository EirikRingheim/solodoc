namespace Solodoc.Shared.Auth;

public record CustomRoleDto(
    Guid Id,
    string Name,
    string? Description,
    string? Color,
    List<string> Permissions,
    List<string>? VisibleModules,
    Dictionary<string, bool>? FeatureFlagOverrides,
    bool IsSystem,
    int MemberCount);

public record CreateCustomRoleRequest(
    string Name,
    string? Description,
    string? Color,
    List<string> Permissions,
    List<string>? VisibleModules = null,
    Dictionary<string, bool>? FeatureFlagOverrides = null);

public record UpdateCustomRoleRequest(
    string Name,
    string? Description,
    string? Color,
    List<string> Permissions,
    List<string>? VisibleModules = null,
    Dictionary<string, bool>? FeatureFlagOverrides = null);

/// <summary>
/// All available permissions grouped by module.
/// </summary>
public static class PermissionDefinitions
{
    public static readonly List<PermissionGroup> AllGroups =
    [
        new("Prosjekter", "projects", [
            new("projects.view", "Se prosjekter"),
            new("projects.create", "Opprette prosjekter"),
            new("projects.edit", "Redigere prosjekter"),
            new("projects.delete", "Slette prosjekter"),
        ]),
        new("Oppdrag", "jobs", [
            new("jobs.view", "Se oppdrag"),
            new("jobs.create", "Opprette oppdrag"),
            new("jobs.edit", "Redigere oppdrag"),
            new("jobs.close", "Lukke oppdrag"),
        ]),
        new("Timeføring", "hours", [
            new("hours.register", "Registrere egne timer"),
            new("hours.approve", "Godkjenne andres timer"),
            new("hours.export", "Eksportere timer"),
        ]),
        new("Avvik", "deviations", [
            new("deviations.report", "Rapportere avvik"),
            new("deviations.assign", "Tildele avvik"),
            new("deviations.close", "Lukke avvik"),
        ]),
        new("Sjekklister", "checklists", [
            new("checklists.complete", "Fylle ut sjekklister"),
            new("checklists.create-template", "Lage maler"),
            new("checklists.approve", "Godkjenne sjekklister"),
        ]),
        new("HMS", "hms", [
            new("hms.sja-create", "Opprette SJA"),
            new("hms.incident-report", "Rapportere hendelser"),
            new("hms.safety-round", "Gjennomføre vernerunder"),
        ]),
        new("Ansatte", "employees", [
            new("employees.view", "Se ansattliste"),
            new("employees.manage", "Administrere ansatte"),
        ]),
        new("Kjemikalier", "chemicals", [
            new("chemicals.view", "Se kjemikalieregister"),
            new("chemicals.edit", "Redigere kjemikalier"),
        ]),
        new("Maskinpark", "machines", [
            new("machines.view", "Se utstyrsregister"),
            new("machines.register", "Registrere utstyr"),
            new("machines.edit", "Redigere utstyr"),
        ]),
        new("Maler", "templates", [
            new("templates.create", "Opprette maler"),
            new("templates.edit", "Redigere maler"),
        ]),
    ];
}

public record PermissionGroup(string Label, string Module, List<Permission> Permissions);
public record Permission(string Key, string Label);
