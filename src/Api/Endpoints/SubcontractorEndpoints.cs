using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Auth;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Auth;

namespace Solodoc.Api.Endpoints;

public static class SubcontractorEndpoints
{
    public static WebApplication MapSubcontractorEndpoints(this WebApplication app)
    {
        app.MapPost("/api/subcontractors/invite", InviteSubcontractor).RequireAuthorization();
        app.MapGet("/api/subcontractors", ListSubcontractors).RequireAuthorization();
        app.MapPost("/api/subcontractors/{id:guid}/revoke", RevokeAccess).RequireAuthorization();
        app.MapGet("/api/subcontractors/invite/{id:guid}", GetInviteDetails).AllowAnonymous();
        app.MapPost("/api/subcontractors/invite/{id:guid}/accept", AcceptSubcontractorInvite).AllowAnonymous();

        return app;
    }

    private static async Task<bool> IsAdminOrPL(Guid personId, Guid tenantId, SolodocDbContext db, CancellationToken ct)
    {
        var m = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == personId && m.TenantId == tenantId && m.State == TenantMembershipState.Active, ct);
        return m?.Role is TenantRole.TenantAdmin or TenantRole.ProjectLeader;
    }

    // ─── Invite subcontractor ───────────────────────────

    private static async Task<IResult> InviteSubcontractor(
        InviteSubcontractorRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var inviterId)) return Results.Unauthorized();

        // Only admins/project leaders can invite subcontractors
        if (!await IsAdminOrPL(inviterId, tenantId, db, ct)) return Results.Forbid();

        var inviterName = user.FindFirstValue("fullName") ?? "Administrator";

        // Validate project exists
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.TenantId == tenantId, ct);
        if (project is null) return Results.BadRequest(new { error = "Prosjekt ikke funnet." });

        // Check for existing pending invite
        var existing = await db.Invitations.FirstOrDefaultAsync(i =>
            i.Email == request.Email && i.TenantId == tenantId && i.ProjectId == request.ProjectId
            && i.State == InvitationState.Pending && !i.IsDeleted, ct);
        if (existing is not null)
            return Results.BadRequest(new { error = "Det finnes allerede en aktiv invitasjon til denne e-posten for dette prosjektet." });

        var expiresAt = request.AccessDays > 0
            ? DateTimeOffset.UtcNow.AddDays(request.AccessDays)
            : DateTimeOffset.UtcNow.AddYears(10); // "unlimited" = 10 years

        var invitation = new Invitation
        {
            TenantId = tenantId,
            Email = request.Email.Trim().ToLowerInvariant(),
            Type = InvitationType.Subcontractor,
            IntendedRole = TenantRole.FieldWorker,
            ProjectId = request.ProjectId,
            State = InvitationState.Pending,
            ExpiresAt = expiresAt,
            InvitedBy = inviterId,
            InvitedByName = inviterName
        };

        db.Invitations.Add(invitation);
        await db.SaveChangesAsync(ct);

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        return Results.Ok(new SubcontractorInviteDto(
            invitation.Id,
            invitation.Email,
            project.Name,
            tenant?.Name ?? "",
            request.AccessDays,
            "Pending",
            invitation.ExpiresAt,
            $"/invite/sub/{invitation.Id}"));
    }

    // ─── List subcontractors for current tenant ─────────

    private static async Task<IResult> ListSubcontractors(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var pid = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var p) ? p : Guid.Empty;
        if (!await IsAdminOrPL(pid, tp.TenantId.Value, db, ct)) return Results.Forbid();

        var accesses = await db.SubcontractorAccesses
            .Where(sa => sa.TenantId == tp.TenantId.Value && !sa.IsDeleted)
            .Include(sa => sa.Person)
            .OrderByDescending(sa => sa.CreatedAt)
            .ToListAsync(ct);

        var projectIds = accesses.Select(a => a.ProjectId).Distinct().ToList();
        var projectNames = await db.Projects
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var result = accesses.Select(sa =>
        {
            projectNames.TryGetValue(sa.ProjectId, out var pName);
            return new SubcontractorAccessDto(
                sa.Id, sa.PersonId, sa.Person.FullName, sa.Person.Email, null,
                sa.ProjectId, pName ?? "Ukjent", sa.State.ToString(),
                sa.HoursRegistrationEnabled, sa.ExpiresAt, sa.CreatedAt);
        }).ToList();

        return Results.Ok(result);
    }

    // ─── Revoke access ──────────────────────────────────

    private static async Task<IResult> RevokeAccess(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var revokePid = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var rp) ? rp : Guid.Empty;
        if (!await IsAdminOrPL(revokePid, tp.TenantId.Value, db, ct)) return Results.Forbid();

        var access = await db.SubcontractorAccesses
            .FirstOrDefaultAsync(sa => sa.Id == id && sa.TenantId == tp.TenantId.Value, ct);
        if (access is null) return Results.NotFound();

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        Guid.TryParse(personIdClaim, out var revokerId);

        access.State = SubcontractorAccessState.Revoked;
        access.RevokedAt = DateTimeOffset.UtcNow;
        access.RevokedBy = revokerId;
        await db.SaveChangesAsync(ct);

        return Results.Ok();
    }

    // ─── Get invite details (public) ────────────────────

    private static async Task<IResult> GetInviteDetails(
        Guid id,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var invitation = await db.Invitations
            .IgnoreQueryFilters()
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Id == id && i.Type == InvitationType.Subcontractor && !i.IsDeleted, ct);

        if (invitation is null)
            return Results.NotFound(new { error = "Invitasjon ikke funnet." });

        string? projectName = null;
        if (invitation.ProjectId.HasValue)
        {
            projectName = await db.Projects.IgnoreQueryFilters()
                .Where(p => p.Id == invitation.ProjectId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct);
        }

        return Results.Ok(new
        {
            id = invitation.Id,
            email = invitation.Email,
            tenantName = invitation.Tenant.Name,
            tenantAccentColor = invitation.Tenant.AccentColor ?? "#4361EE",
            projectName = projectName ?? "Ukjent prosjekt",
            invitedByName = invitation.InvitedByName,
            expiresAt = invitation.ExpiresAt,
            state = invitation.State.ToString(),
            isExpired = invitation.ExpiresAt < DateTimeOffset.UtcNow
        });
    }

    // ─── Accept subcontractor invite ────────────────────

    private static async Task<IResult> AcceptSubcontractorInvite(
        Guid id,
        AcceptInvitationRequest request,
        SolodocDbContext db,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        CancellationToken ct)
    {
        var invitation = await db.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id && i.Type == InvitationType.Subcontractor && !i.IsDeleted, ct);

        if (invitation is null)
            return Results.NotFound(new { error = "Invitasjon ikke funnet." });

        if (invitation.State != InvitationState.Pending)
            return Results.BadRequest(new { error = "Denne invitasjonen er allerede brukt." });

        if (invitation.ExpiresAt < DateTimeOffset.UtcNow)
            return Results.BadRequest(new { error = "Invitasjonen har utlopt." });

        // Find or create person
        var person = await db.Persons
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Email == invitation.Email, ct);

        if (person is null)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Navn og passord er påkrevd for nye brukere." });

            person = new Person
            {
                Email = invitation.Email,
                FullName = request.FullName.Trim(),
                PasswordHash = passwordHasher.Hash(request.Password),
                State = PersonState.Active,
                EmailVerified = true,
                TimeZoneId = "Europe/Oslo"
            };
            db.Persons.Add(person);
        }

        // Create subcontractor access for the project
        var accessDays = (invitation.ExpiresAt - DateTimeOffset.UtcNow).TotalDays;
        var existingAccess = await db.SubcontractorAccesses
            .FirstOrDefaultAsync(sa => sa.PersonId == person.Id && sa.ProjectId == invitation.ProjectId && sa.TenantId == invitation.TenantId, ct);

        if (existingAccess is not null)
        {
            existingAccess.State = SubcontractorAccessState.Active;
            existingAccess.ExpiresAt = invitation.ExpiresAt;
            existingAccess.RevokedAt = null;
            existingAccess.RevokedBy = null;
        }
        else
        {
            db.SubcontractorAccesses.Add(new SubcontractorAccess
            {
                PersonId = person.Id,
                TenantId = invitation.TenantId,
                ProjectId = invitation.ProjectId!.Value,
                State = SubcontractorAccessState.Active,
                HoursRegistrationEnabled = true,
                ExpiresAt = invitation.ExpiresAt
            });
        }

        // Also create a tenant membership as subcontractor role (FieldWorker for now)
        var existingMembership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == person.Id && m.TenantId == invitation.TenantId, ct);

        if (existingMembership is null)
        {
            db.TenantMemberships.Add(new TenantMembership
            {
                PersonId = person.Id,
                TenantId = invitation.TenantId,
                Role = TenantRole.FieldWorker,
                State = TenantMembershipState.Active
            });
        }

        // Mark invitation as accepted
        invitation.State = InvitationState.Accepted;
        invitation.AcceptedAt = DateTimeOffset.UtcNow;
        invitation.AcceptedByPersonId = person.Id;

        await db.SaveChangesAsync(ct);

        // Generate tokens
        var accessToken = tokenService.GenerateAccessToken(person, invitation.TenantId, "field-worker");
        var refreshToken = tokenService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            PersonId = person.Id,
            Token = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync(ct);

        return Results.Ok(new AcceptInvitationResponse(
            person.Email,
            invitation.TenantId,
            new AuthResponse(accessToken, refreshToken, DateTimeOffset.UtcNow.AddMinutes(15),
                new PersonDto(person.Id, person.Email, person.FullName))));
    }
}
