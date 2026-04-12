using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Application.Services;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Enums;
using Solodoc.Application.Auth;
using Solodoc.Infrastructure.Auth;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Auth;
using Solodoc.Shared.Onboarding;

namespace Solodoc.Api.Endpoints;

public static class OnboardingEndpoints
{
    public static WebApplication MapOnboardingEndpoints(this WebApplication app)
    {
        app.MapGet("/api/onboarding/status", GetStatus).RequireAuthorization();
        app.MapPost("/api/onboarding/create-tenant", CreateTenantOnboarding).RequireAuthorization();
        app.MapPost("/api/onboarding/step1", SaveStep1).RequireAuthorization();
        app.MapPost("/api/onboarding/step2", SaveStep2).RequireAuthorization();
        app.MapPost("/api/onboarding/step3", SaveStep3).RequireAuthorization();
        app.MapPost("/api/onboarding/complete", Complete).RequireAuthorization();
        app.MapPost("/api/onboarding/reset", ResetOnboarding).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetStatus(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        // If no tenant, return "needs onboarding" status
        if (tp.TenantId is null)
        {
            return Results.Ok(new OnboardingStatusDto(false, null, null, [], "none", null));
        }

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        if (tenant is null)
            return Results.Ok(new OnboardingStatusDto(false, null, null, [], "none", null));

        var modules = new List<string>();
        if (!string.IsNullOrEmpty(tenant.EnabledModules))
        {
            try { modules = JsonSerializer.Deserialize<List<string>>(tenant.EnabledModules) ?? []; } catch { }
        }

        return Results.Ok(new OnboardingStatusDto(
            tenant.OnboardingCompleted,
            tenant.IndustryType,
            tenant.CompanySize,
            modules,
            tenant.SubscriptionTier,
            tenant.TrialEndsAt));
    }

    private static async Task<IResult> CreateTenantOnboarding(
        CreateTenantOnboardingRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITokenService tokenService,
        CancellationToken ct)
    {
        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId))
            return Results.Unauthorized();

        // Check if person already has a tenant
        var existing = await db.TenantMemberships
            .AnyAsync(m => m.PersonId == personId && m.State == TenantMembershipState.Active, ct);
        if (existing)
            return Results.BadRequest(new { error = "Du har allerede en bedrift." });

        var tenant = new Tenant
        {
            Name = request.CompanyName,
            OrgNumber = request.OrgNumber ?? "",
            BusinessType = Domain.Enums.BusinessType.AS,
            IndustryType = request.IndustryType,
            CompanySize = request.CompanySize,
            SubscriptionTier = "trial",
            TrialStartedAt = DateTimeOffset.UtcNow,
            TrialEndsAt = DateTimeOffset.UtcNow.AddDays(30),
            DefaultTimeZoneId = "Europe/Oslo"
        };
        db.Tenants.Add(tenant);

        var membership = new TenantMembership
        {
            PersonId = personId,
            TenantId = tenant.Id,
            Role = TenantRole.TenantAdmin,
            State = TenantMembershipState.Active
        };
        db.TenantMemberships.Add(membership);

        await db.SaveChangesAsync(ct);

        // Generate new tokens with tenant-admin role so client immediately has correct permissions
        var person = await db.Persons.FirstAsync(p => p.Id == personId, ct);
        var accessToken = tokenService.GenerateAccessToken(person, tenant.Id, "tenant-admin");
        var refreshTokenValue = tokenService.GenerateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            PersonId = personId,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            tenantId = tenant.Id,
            accessToken,
            refreshToken = refreshTokenValue
        });
    }

    private static async Task<IResult> SaveStep1(
        SaveOnboardingStep1Request request,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        if (tenant is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.CompanyName))
            tenant.Name = request.CompanyName;
        tenant.IndustryType = request.IndustryType;
        tenant.CompanySize = request.CompanySize;

        // Start trial if not started
        if (tenant.TrialStartedAt is null)
        {
            tenant.TrialStartedAt = DateTimeOffset.UtcNow;
            tenant.TrialEndsAt = DateTimeOffset.UtcNow.AddDays(30);
            tenant.SubscriptionTier = "trial";
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> SaveStep2(
        SaveOnboardingStep2Request request,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        if (tenant is null) return Results.NotFound();

        tenant.EnabledModules = JsonSerializer.Serialize(request.EnabledModules ?? []);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> SaveStep3(
        SaveOnboardingStep3Request request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        IEmailService emailService,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId)) return Results.Unauthorized();

        var inviterName = user.FindFirstValue("fullName") ?? "Administrator";
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        if (tenant is null) return Results.NotFound();

        var created = 0;
        foreach (var invite in request.Invites ?? [])
        {
            if (string.IsNullOrWhiteSpace(invite.Email)) continue;

            var role = invite.Role?.ToLowerInvariant() switch
            {
                "admin" or "tenantadmin" => TenantRole.TenantAdmin,
                "prosjektleder" or "project-leader" => TenantRole.ProjectLeader,
                _ => TenantRole.FieldWorker
            };

            var existing = await db.Invitations
                .AnyAsync(i => i.Email == invite.Email.ToLowerInvariant()
                    && i.TenantId == tp.TenantId.Value
                    && i.State == InvitationState.Pending, ct);
            if (existing) continue;

            var invitation = new Invitation
            {
                TenantId = tp.TenantId.Value,
                Email = invite.Email.ToLowerInvariant(),
                IntendedRole = role,
                InvitedByName = inviterName,
                InvitedBy = personId,
                State = InvitationState.Pending,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
            };
            db.Invitations.Add(invitation);
            created++;

            // Send email (fire and forget)
            try
            {
                await emailService.SendInvitationAsync(
                    invite.Email, inviterName, tenant.Name,
                    role.ToString(), invitation.Id, ct);
            }
            catch { }
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { invited = created });
    }

    private static async Task<IResult> Complete(
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        if (tenant is null) return Results.NotFound();

        tenant.OnboardingCompleted = true;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ResetOnboarding(
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        if (tenant is null) return Results.NotFound();

        tenant.OnboardingCompleted = false;
        tenant.IndustryType = null;
        tenant.CompanySize = null;
        tenant.EnabledModules = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
