using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Application.Services;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Auth;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Onboarding;

namespace Solodoc.Api.Endpoints;

public static class OnboardingEndpoints
{
    public static WebApplication MapOnboardingEndpoints(this WebApplication app)
    {
        app.MapGet("/api/onboarding/status", GetStatus).RequireAuthorization();
        app.MapPost("/api/onboarding/step1", SaveStep1).RequireAuthorization();
        app.MapPost("/api/onboarding/step2", SaveStep2).RequireAuthorization();
        app.MapPost("/api/onboarding/step3", SaveStep3).RequireAuthorization();
        app.MapPost("/api/onboarding/complete", Complete).RequireAuthorization();
        app.MapPost("/api/onboarding/reset", ResetOnboarding).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetStatus(
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        if (tenant is null) return Results.NotFound();

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
