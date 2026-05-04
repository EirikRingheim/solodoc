using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Application.Services;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Entities.Employees;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Employees;

namespace Solodoc.Api.Endpoints;

public static class EmployeeEndpoints
{
    public static WebApplication MapEmployeeEndpoints(this WebApplication app)
    {
        // Employee management (admin)
        app.MapGet("/api/employees", ListEmployees).RequireAuthorization();
        app.MapGet("/api/employees/{personId:guid}", GetEmployeeDetail).RequireAuthorization();
        app.MapGet("/api/employees/invitations", ListInvitations).RequireAuthorization();
        app.MapPost("/api/employees/invite", InviteEmployee).RequireAuthorization();
        app.MapPatch("/api/employees/{personId:guid}/suspend", SuspendEmployee).RequireAuthorization();
        app.MapPatch("/api/employees/{personId:guid}/activate", ActivateEmployee).RequireAuthorization();
        app.MapDelete("/api/employees/{personId:guid}", RemoveEmployee).RequireAuthorization();
        app.MapPatch("/api/employees/{personId:guid}/role", ChangeEmployeeRole).RequireAuthorization();

        // Project crew (derived from time entries)
        app.MapGet("/api/projects/{projectId:guid}/crew", GetProjectCrew).RequireAuthorization();

        // Certifications
        app.MapGet("/api/employees/{personId:guid}/certifications", ListCertifications).RequireAuthorization();
        app.MapPost("/api/employees/{personId:guid}/certifications", AddCertification).RequireAuthorization();

        // Training
        app.MapGet("/api/employees/{personId:guid}/training", ListTraining).RequireAuthorization();
        app.MapPost("/api/employees/{personId:guid}/training", CreateTraining).RequireAuthorization();

        // Self-service profile
        app.MapGet("/api/profile", GetProfile).RequireAuthorization();
        app.MapPut("/api/profile", UpdateProfile).RequireAuthorization();
        app.MapPost("/api/profile/gps-consent", UpdateGpsConsent).RequireAuthorization();
        app.MapGet("/api/profile/gps-consent", GetGpsConsent).RequireAuthorization();

        // Vacation
        app.MapGet("/api/employees/{personId:guid}/vacation", GetVacation).RequireAuthorization();
        app.MapPost("/api/employees/{personId:guid}/vacation", CreateVacationEntry).RequireAuthorization();
        app.MapPatch("/api/employees/{personId:guid}/vacation/{entryId:guid}/approve", ApproveVacationEntry).RequireAuthorization();

        return app;
    }

    // ─── Helpers ────────────────────────────────────────────────────

    private static (Guid? personId, bool valid) GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub");
        if (Guid.TryParse(claim, out var personId))
            return (personId, true);
        return (null, false);
    }

    private static string RoleToString(TenantRole role) => role switch
    {
        TenantRole.TenantAdmin => "Admin",
        TenantRole.ProjectLeader => "Prosjektleder",
        TenantRole.FieldWorker => "Feltarbeider",
        _ => "Feltarbeider"
    };

    private static TenantRole? ParseRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return null;
        return role.ToLowerInvariant() switch
        {
            "admin" or "tenantadmin" => TenantRole.TenantAdmin,
            "prosjektleder" or "projectleader" => TenantRole.ProjectLeader,
            "feltarbeider" or "fieldworker" => TenantRole.FieldWorker,
            _ => null
        };
    }

    private static string VacationStatusToString(VacationStatus status) => status switch
    {
        VacationStatus.Pending => "Venter",
        VacationStatus.Approved => "Godkjent",
        VacationStatus.Rejected => "Avvist",
        _ => "Venter"
    };

    /// <summary>
    /// Returns the caller's TenantMembership if they have an admin-level role
    /// (TenantAdmin or ProjectLeader). Returns null when forbidden.
    /// </summary>
    private static async Task<TenantMembership?> RequireAdminRole(
        Guid personId, SolodocDbContext db, CancellationToken ct, Guid? tenantId = null)
    {
        var query = db.TenantMemberships
            .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active);

        if (tenantId.HasValue)
            query = query.Where(m => m.TenantId == tenantId.Value);

        var membership = await query.FirstOrDefaultAsync(ct);

        if (membership is null)
            return null;

        if (membership.Role != TenantRole.TenantAdmin && membership.Role != TenantRole.ProjectLeader)
            return null;

        return membership;
    }

    // ─── Project Crew ─────────────────────────────────────────────

    private static async Task<IResult> GetProjectCrew(
        Guid projectId,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var tenantId = tenantProvider.TenantId.Value;

        // Crew = distinct persons who have time entries OR check-ins on this project
        var fromHours = await db.TimeEntries
            .Where(t => t.TenantId == tenantId && t.ProjectId == projectId && !t.IsDeleted)
            .Select(t => t.PersonId)
            .ToListAsync(ct);

        var fromCheckIns = await db.WorksiteCheckIns
            .Where(c => c.TenantId == tenantId && c.ProjectId == projectId && !c.IsDeleted)
            .Where(c => c.PersonId != null)
            .Select(c => c.PersonId!.Value)
            .ToListAsync(ct);

        var crewPersonIds = fromHours.Concat(fromCheckIns).Distinct().ToList();

        if (crewPersonIds.Count == 0)
            return Results.Ok(new List<EmployeeListItemDto>());

        var crew = await db.TenantMemberships
            .Where(m => m.TenantId == tenantId && crewPersonIds.Contains(m.PersonId))
            .Join(db.Persons,
                m => m.PersonId,
                p => p.Id,
                (m, p) => new EmployeeListItemDto(
                    p.Id,
                    p.FullName,
                    p.Email,
                    RoleToString(m.Role),
                    0, 0, 0))
            .ToListAsync(ct);

        return Results.Ok(crew);
    }

    // ─── Employee Management (Admin) ────────────────────────────────

    private static async Task<IResult> ListEmployees(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var admin = await RequireAdminRole(personId!.Value, db, ct, tenantProvider.TenantId);
        if (admin is null) return Results.Forbid();

        var tenantId = tenantProvider.TenantId.Value;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soonThreshold = today.AddDays(30);

        var employees = await db.TenantMemberships
            .Where(m => m.TenantId == tenantId && m.State != TenantMembershipState.Removed)
            .Join(db.Persons,
                m => m.PersonId,
                p => p.Id,
                (m, p) => new { Membership = m, Person = p })
            .Select(x => new EmployeeListItemDto(
                x.Person.Id,
                x.Person.FullName,
                x.Person.Email,
                RoleToString(x.Membership.Role),
                db.EmployeeCertifications.Count(c => c.PersonId == x.Person.Id && c.TenantId == tenantId),
                db.EmployeeCertifications.Count(c =>
                    c.PersonId == x.Person.Id &&
                    c.TenantId == tenantId &&
                    c.ExpiryDate.HasValue &&
                    c.ExpiryDate.Value > today &&
                    c.ExpiryDate.Value <= soonThreshold),
                db.EmployeeCertifications.Count(c =>
                    c.PersonId == x.Person.Id &&
                    c.TenantId == tenantId &&
                    c.ExpiryDate.HasValue &&
                    c.ExpiryDate.Value < today)))
            .ToListAsync(ct);

        return Results.Ok(employees);
    }

    private static async Task<IResult> GetEmployeeDetail(
        Guid personId,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var admin = await RequireAdminRole(callerId!.Value, db, ct);
        if (admin is null) return Results.Forbid();

        var tenantId = tenantProvider.TenantId.Value;

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.PersonId == personId &&
                m.TenantId == tenantId &&
                m.State != TenantMembershipState.Removed, ct);

        if (membership is null) return Results.NotFound();

        var person = await db.Persons.FirstOrDefaultAsync(p => p.Id == personId, ct);
        if (person is null) return Results.NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soonThreshold = today.AddDays(30);

        // Certifications follow the person across tenants (per spec)
        var certifications = await db.EmployeeCertifications
            .Where(c => c.PersonId == personId)
            .OrderBy(c => c.ExpiryDate)
            .Select(c => new CertificationDto(
                c.Id,
                c.Name,
                c.Type,
                c.IssuedBy,
                c.ExpiryDate,
                c.ExpiryDate.HasValue && c.ExpiryDate.Value < today,
                c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= soonThreshold,
                c.FileKey))
            .ToListAsync(ct);

        var trainings = await db.InternalTrainings
            .Where(t => t.TraineeId == personId && t.TenantId == tenantId)
            .OrderByDescending(t => t.Date)
            .Select(t => new TrainingDto(
                t.Id,
                t.Topic,
                db.Persons.Where(p => p.Id == t.TrainerId).Select(p => p.FullName).FirstOrDefault() ?? "",
                t.Date,
                t.DurationHours))
            .ToListAsync(ct);

        var dto = new EmployeeDetailDto(
            person.Id,
            person.FullName,
            person.Email,
            person.PhoneNumber,
            RoleToString(membership.Role),
            person.TimeZoneId,
            certifications,
            trainings);

        return Results.Ok(dto);
    }

    private static async Task<IResult> ListInvitations(
        ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        var (pid, valid) = GetPersonId(user);
        if (!valid || tp.TenantId is null) return Results.Unauthorized();

        var invitations = await db.Invitations
            .Where(i => i.TenantId == tp.TenantId.Value)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id, i.Email,
                Role = i.IntendedRole.ToString(),
                State = i.State.ToString(),
                i.InvitedByName,
                i.CreatedAt,
                i.ExpiresAt
            })
            .ToListAsync(ct);

        return Results.Ok(invitations);
    }

    private static async Task<IResult> InviteEmployee(
        InviteEmployeeRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        IEmailService emailService,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var admin = await RequireAdminRole(personId!.Value, db, ct, tenantProvider.TenantId);
        if (admin is null) return Results.Forbid();

        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "E-post er påkrevd." });

        var role = ParseRole(request.Role);
        if (role is null)
            return Results.BadRequest(new { error = $"Ugyldig rolle: {request.Role}" });

        // Check if there's already a pending invitation
        var existing = await db.Invitations
            .AnyAsync(i =>
                i.TenantId == tenantProvider.TenantId.Value &&
                i.Email == request.Email &&
                i.State == InvitationState.Pending, ct);

        if (existing)
            return Results.BadRequest(new { error = "Det finnes allerede en ventende invitasjon for denne e-posten." });

        var inviterName = await db.Persons
            .Where(p => p.Id == personId!.Value)
            .Select(p => p.FullName)
            .FirstOrDefaultAsync(ct) ?? "";

        var invitation = new Invitation
        {
            TenantId = tenantProvider.TenantId.Value,
            Email = request.Email.Trim().ToLowerInvariant(),
            Type = InvitationType.TenantMember,
            IntendedRole = role.Value,
            State = InvitationState.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            InvitedBy = personId!.Value,
            InvitedByName = inviterName
        };

        db.Invitations.Add(invitation);
        await db.SaveChangesAsync(ct);

        // Send invitation email
        var tenantName = await db.Tenants
            .Where(t => t.Id == tenantProvider.TenantId.Value)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? "Solodoc";

        var roleDisplay = role.Value switch
        {
            TenantRole.TenantAdmin => "Administrator",
            TenantRole.ProjectLeader => "Prosjektleder",
            TenantRole.FieldWorker => "Feltarbeider",
            _ => "Bruker"
        };

        try
        {
            await emailService.SendInvitationAsync(request.Email, inviterName, tenantName, roleDisplay, invitation.Id, ct);
        }
        catch
        {
            // Invitation saved but email failed — return success with warning
            return Results.Ok(new { id = invitation.Id, warning = "Invitasjon opprettet, men e-post kunne ikke sendes. Del lenken manuelt." });
        }

        return Results.Created($"/api/employees/invite/{invitation.Id}", new { id = invitation.Id });
    }

    private static async Task<IResult> SuspendEmployee(
        Guid personId,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var admin = await RequireAdminRole(callerId!.Value, db, ct);
        if (admin is null) return Results.Forbid();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.PersonId == personId &&
                m.TenantId == tenantProvider.TenantId.Value &&
                m.State == TenantMembershipState.Active, ct);

        if (membership is null)
            return Results.NotFound();

        membership.State = TenantMembershipState.Suspended;
        membership.SuspendedAt = DateTimeOffset.UtcNow;
        membership.SuspendedBy = callerId!.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { status = "Suspended" });
    }

    private static async Task<IResult> ActivateEmployee(
        Guid personId,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var admin = await RequireAdminRole(callerId!.Value, db, ct);
        if (admin is null) return Results.Forbid();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.PersonId == personId &&
                m.TenantId == tenantProvider.TenantId.Value &&
                m.State == TenantMembershipState.Suspended, ct);

        if (membership is null)
            return Results.NotFound();

        membership.State = TenantMembershipState.Active;
        membership.SuspendedAt = null;
        membership.SuspendedBy = null;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { status = "Active" });
    }

    private static async Task<IResult> RemoveEmployee(
        Guid personId,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var admin = await RequireAdminRole(callerId!.Value, db, ct);
        if (admin is null) return Results.Forbid();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.PersonId == personId &&
                m.TenantId == tenantProvider.TenantId.Value &&
                m.State != TenantMembershipState.Removed, ct);

        if (membership is null)
            return Results.NotFound();

        membership.State = TenantMembershipState.Removed;
        membership.RemovedAt = DateTimeOffset.UtcNow;
        membership.RemovedBy = callerId!.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { status = "Removed" });
    }

    // ─── Change Role ────────────────────────────────────────────────

    private static async Task<IResult> ChangeEmployeeRole(
        Guid personId,
        ChangeRoleRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var admin = await RequireAdminRole(callerId!.Value, db, ct);
        if (admin is null) return Results.Forbid();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.PersonId == personId &&
                m.TenantId == tenantProvider.TenantId.Value &&
                m.State == TenantMembershipState.Active, ct);

        if (membership is null)
            return Results.NotFound();

        // Don't allow changing your own role
        if (personId == callerId.Value)
            return Results.BadRequest(new { error = "Du kan ikke endre din egen rolle." });

        var newRole = ParseRole(request.Role);
        if (newRole is null)
            return Results.BadRequest(new { error = $"Ugyldig rolle: {request.Role}" });

        membership.Role = newRole.Value;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { role = RoleToString(newRole.Value) });
    }

    // ─── Certifications ─────────────────────────────────────────────

    private static async Task<IResult> ListCertifications(
        Guid personId,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var tenantId = tenantProvider.TenantId.Value;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soonThreshold = today.AddDays(30);

        // Certifications follow the person across tenants
        var certs = await db.EmployeeCertifications
            .Where(c => c.PersonId == personId)
            .OrderBy(c => c.ExpiryDate)
            .Select(c => new CertificationDto(
                c.Id,
                c.Name,
                c.Type,
                c.IssuedBy,
                c.ExpiryDate,
                c.ExpiryDate.HasValue && c.ExpiryDate.Value < today,
                c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= soonThreshold,
                c.FileKey))
            .ToListAsync(ct);

        return Results.Ok(certs);
    }

    private static async Task<IResult> AddCertification(
        Guid personId,
        CreateCertificationRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var admin = await RequireAdminRole(callerId!.Value, db, ct);
        if (admin is null) return Results.Forbid();

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Sertifikatnavn er påkrevd." });

        var tenantId = tenantProvider.TenantId.Value;

        // Verify the person belongs to the tenant
        var membershipExists = await db.TenantMemberships
            .AnyAsync(m =>
                m.PersonId == personId &&
                m.TenantId == tenantId &&
                m.State != TenantMembershipState.Removed, ct);

        if (!membershipExists)
            return Results.NotFound();

        var cert = new EmployeeCertification
        {
            PersonId = personId,
            TenantId = tenantId,
            Name = request.Name,
            Type = request.Type ?? string.Empty,
            IssuedBy = request.IssuedBy,
            ExpiryDate = request.ExpiryDate,
            FileKey = request.FileKey
        };

        db.EmployeeCertifications.Add(cert);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/employees/{personId}/certifications/{cert.Id}", new { id = cert.Id });
    }

    // ─── Training ───────────────────────────────────────────────────

    private static async Task<IResult> ListTraining(
        Guid personId,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var tenantId = tenantProvider.TenantId.Value;

        var trainings = await db.InternalTrainings
            .Where(t => t.TraineeId == personId && t.TenantId == tenantId)
            .OrderByDescending(t => t.Date)
            .Select(t => new TrainingDto(
                t.Id,
                t.Topic,
                db.Persons.Where(p => p.Id == t.TrainerId).Select(p => p.FullName).FirstOrDefault() ?? "",
                t.Date,
                t.DurationHours))
            .ToListAsync(ct);

        return Results.Ok(trainings);
    }

    private static async Task<IResult> CreateTraining(
        Guid personId,
        CreateTrainingRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var admin = await RequireAdminRole(callerId!.Value, db, ct);
        if (admin is null) return Results.Forbid();

        if (string.IsNullOrWhiteSpace(request.Topic))
            return Results.BadRequest(new { error = "Emne er påkrevd." });

        var tenantId = tenantProvider.TenantId.Value;

        // Verify the person belongs to the tenant
        var membershipExists = await db.TenantMemberships
            .AnyAsync(m =>
                m.PersonId == personId &&
                m.TenantId == tenantId &&
                m.State != TenantMembershipState.Removed, ct);

        if (!membershipExists)
            return Results.NotFound();

        // Verify trainer belongs to the tenant
        var trainerExists = await db.TenantMemberships
            .AnyAsync(m =>
                m.PersonId == request.TrainerId &&
                m.TenantId == tenantId &&
                m.State == TenantMembershipState.Active, ct);

        if (!trainerExists)
            return Results.BadRequest(new { error = "Instruktøren finnes ikke i denne organisasjonen." });

        var training = new InternalTraining
        {
            TenantId = tenantId,
            Topic = request.Topic,
            TrainerId = request.TrainerId,
            TraineeId = personId,
            Date = request.Date,
            DurationHours = request.DurationHours,
            Notes = request.Notes
        };

        db.InternalTrainings.Add(training);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/employees/{personId}/training/{training.Id}", new { id = training.Id });
    }

    // ─── Self-Service Profile ───────────────────────────────────────

    private static async Task<IResult> GetProfile(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var person = await db.Persons
            .Where(p => p.Id == personId!.Value)
            .Select(p => new ProfileDto(
                p.Id,
                p.FullName,
                p.Email,
                p.PhoneNumber,
                p.TimeZoneId))
            .FirstOrDefaultAsync(ct);

        return person is not null ? Results.Ok(person) : Results.NotFound();
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var person = await db.Persons
            .FirstOrDefaultAsync(p => p.Id == personId!.Value, ct);

        if (person is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(request.FullName))
            return Results.BadRequest(new { error = "Fullt navn er påkrevd." });

        person.FullName = request.FullName;
        person.PhoneNumber = request.Phone;
        person.TimeZoneId = request.TimeZoneId;

        await db.SaveChangesAsync(ct);

        var dto = new ProfileDto(
            person.Id,
            person.FullName,
            person.Email,
            person.PhoneNumber,
            person.TimeZoneId);

        return Results.Ok(dto);
    }

    // ─── Vacation ───────────────────────────────────────────────────

    private static async Task<IResult> GetVacation(
        Guid personId,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var tenantId = tenantProvider.TenantId.Value;
        var currentYear = DateTime.UtcNow.Year;

        var balance = await db.VacationBalances
            .Where(b => b.PersonId == personId && b.TenantId == tenantId && b.Year == currentYear)
            .Select(b => new VacationBalanceDto(
                b.Year,
                b.AnnualAllowanceDays,
                b.CarriedOverDays,
                b.UsedDays,
                b.AnnualAllowanceDays + b.CarriedOverDays - b.UsedDays))
            .FirstOrDefaultAsync(ct);

        var entries = await db.VacationEntries
            .Where(e => e.PersonId == personId && e.TenantId == tenantId &&
                        e.StartDate.Year == currentYear)
            .OrderByDescending(e => e.StartDate)
            .Select(e => new VacationEntryDto(
                e.Id,
                e.StartDate,
                e.EndDate,
                e.Days,
                VacationStatusToString(e.Status),
                e.ApprovedById.HasValue ? db.Persons.Where(p => p.Id == e.ApprovedById.Value).Select(p => p.FullName).FirstOrDefault() : null,
                e.ApprovedAt))
            .ToListAsync(ct);

        return Results.Ok(new VacationOverviewDto(balance, entries));
    }

    private static async Task<IResult> CreateVacationEntry(
        Guid personId,
        CreateVacationEntryRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var tenantId = tenantProvider.TenantId.Value;

        if (request.Days <= 0)
            return Results.BadRequest(new { error = "Antall dager må være større enn 0." });

        if (request.EndDate < request.StartDate)
            return Results.BadRequest(new { error = "Sluttdato kan ikke være før startdato." });

        // Verify the person belongs to the tenant
        var membershipExists = await db.TenantMemberships
            .AnyAsync(m =>
                m.PersonId == personId &&
                m.TenantId == tenantId &&
                m.State != TenantMembershipState.Removed, ct);

        if (!membershipExists)
            return Results.NotFound();

        var entry = new VacationEntry
        {
            PersonId = personId,
            TenantId = tenantId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Days = request.Days,
            Status = VacationStatus.Pending
        };

        db.VacationEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/employees/{personId}/vacation/{entry.Id}", new { id = entry.Id });
    }

    private static async Task<IResult> ApproveVacationEntry(
        Guid personId,
        Guid entryId,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (callerId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var admin = await RequireAdminRole(callerId!.Value, db, ct);
        if (admin is null) return Results.Forbid();

        var tenantId = tenantProvider.TenantId.Value;

        var entry = await db.VacationEntries
            .FirstOrDefaultAsync(e =>
                e.Id == entryId &&
                e.PersonId == personId &&
                e.TenantId == tenantId, ct);

        if (entry is null)
            return Results.NotFound();

        if (entry.Status != VacationStatus.Pending)
            return Results.BadRequest(new { error = "Kun ventende ferieposter kan godkjennes." });

        entry.Status = VacationStatus.Approved;
        entry.ApprovedById = callerId!.Value;
        entry.ApprovedAt = DateTimeOffset.UtcNow;

        // Update vacation balance used days
        var balance = await db.VacationBalances
            .FirstOrDefaultAsync(b =>
                b.PersonId == personId &&
                b.TenantId == tenantId &&
                b.Year == entry.StartDate.Year, ct);

        if (balance is not null)
        {
            balance.UsedDays += entry.Days;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { status = "Godkjent" });
    }

    // ── GPS Consent ──

    private static async Task<IResult> GetGpsConsent(
        ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tp.TenantId is null) return Results.Unauthorized();

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == personId!.Value && m.TenantId == tp.TenantId.Value, ct);

        return Results.Ok(new
        {
            gpsEnabledByTenant = tenant?.GpsEnabled ?? false,
            consentGiven = membership?.GpsConsent ?? false,
            consentChangedAt = membership?.GpsConsentChangedAt
        });
    }

    private static async Task<IResult> UpdateGpsConsent(
        GpsConsentRequest request,
        ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tp.TenantId is null) return Results.Unauthorized();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == personId!.Value && m.TenantId == tp.TenantId.Value, ct);
        if (membership is null) return Results.NotFound();

        membership.GpsConsent = request.Consent;
        membership.GpsConsentChangedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { consentGiven = membership.GpsConsent });
    }
}
