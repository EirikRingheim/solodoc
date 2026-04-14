using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Expenses;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Expenses;

namespace Solodoc.Api.Endpoints;

public static class ExpenseSettingsEndpoints
{
    public static WebApplication MapExpenseSettingsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/expense-settings", GetSettings).RequireAuthorization();
        app.MapPut("/api/expense-settings", UpdateSettings).RequireAuthorization();
        app.MapGet("/api/travel-expense-rates", GetRates).RequireAuthorization();
        app.MapPost("/api/travel-expense-rates", CreateRate).RequireAuthorization();
        app.MapPut("/api/travel-expense-rates/{id:guid}", UpdateRate).RequireAuthorization();

        return app;
    }

    private static async Task<bool> IsAdmin(ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return false;
        var pid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(pid, out var personId)) return false;
        var m = await db.TenantMemberships.FirstOrDefaultAsync(
            m => m.PersonId == personId && m.TenantId == tp.TenantId.Value && m.State == TenantMembershipState.Active, ct);
        return m?.Role == TenantRole.TenantAdmin;
    }

    private static async Task<IResult> GetSettings(SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var s = await db.ExpenseSettingsTable.FirstOrDefaultAsync(x => x.TenantId == tp.TenantId.Value, ct);
        return Results.Ok(s is not null
            ? new ExpenseSettingsDto(s.RequireDate, s.RequireDescription, s.RequireCategory, s.RequireProject)
            : new ExpenseSettingsDto(true, false, false, false));
    }

    private static async Task<IResult> UpdateSettings(
        UpdateExpenseSettingsRequest request, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (!await IsAdmin(user, db, tp, ct)) return Results.Forbid();

        var s = await db.ExpenseSettingsTable.FirstOrDefaultAsync(x => x.TenantId == tp.TenantId!.Value, ct);
        if (s is null)
        {
            s = new ExpenseSettings { TenantId = tp.TenantId!.Value };
            db.ExpenseSettingsTable.Add(s);
        }
        s.RequireDate = request.RequireDate;
        s.RequireDescription = request.RequireDescription;
        s.RequireCategory = request.RequireCategory;
        s.RequireProject = request.RequireProject;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> GetRates(SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var rates = await db.TravelExpenseRates
            .Where(r => r.TenantId == tp.TenantId.Value)
            .OrderByDescending(r => r.Year)
            .Select(r => new TravelExpenseRateDto(
                r.Id, r.Year, r.Diet6To12Hours, r.Diet12PlusHours, r.DietOvernight,
                r.BreakfastDeductionPct, r.LunchDeductionPct, r.DinnerDeductionPct,
                r.MileagePerKm, r.PassengerSurchargePerKm, r.ForestRoadSurchargePerKm,
                r.TrailerSurchargePerKm, r.UndocumentedNightRate, r.IsActive))
            .ToListAsync(ct);
        return Results.Ok(rates);
    }

    private static async Task<IResult> CreateRate(
        CreateTravelExpenseRateRequest request, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (!await IsAdmin(user, db, tp, ct)) return Results.Forbid();

        var exists = await db.TravelExpenseRates.AnyAsync(
            r => r.TenantId == tp.TenantId!.Value && r.Year == request.Year, ct);
        if (exists)
            return Results.BadRequest(new { error = $"Satser for {request.Year} finnes allerede." });

        var rate = new TravelExpenseRate
        {
            TenantId = tp.TenantId!.Value,
            Year = request.Year,
            Diet6To12Hours = request.Diet6To12Hours,
            Diet12PlusHours = request.Diet12PlusHours,
            DietOvernight = request.DietOvernight,
            BreakfastDeductionPct = request.BreakfastDeductionPct,
            LunchDeductionPct = request.LunchDeductionPct,
            DinnerDeductionPct = request.DinnerDeductionPct,
            MileagePerKm = request.MileagePerKm,
            PassengerSurchargePerKm = request.PassengerSurchargePerKm,
            ForestRoadSurchargePerKm = request.ForestRoadSurchargePerKm,
            TrailerSurchargePerKm = request.TrailerSurchargePerKm,
            UndocumentedNightRate = request.UndocumentedNightRate
        };
        db.TravelExpenseRates.Add(rate);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/travel-expense-rates/{rate.Id}", new { id = rate.Id });
    }

    private static async Task<IResult> UpdateRate(
        Guid id, CreateTravelExpenseRateRequest request, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (!await IsAdmin(user, db, tp, ct)) return Results.Forbid();

        var rate = await db.TravelExpenseRates.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tp.TenantId!.Value, ct);
        if (rate is null) return Results.NotFound();

        rate.Diet6To12Hours = request.Diet6To12Hours;
        rate.Diet12PlusHours = request.Diet12PlusHours;
        rate.DietOvernight = request.DietOvernight;
        rate.BreakfastDeductionPct = request.BreakfastDeductionPct;
        rate.LunchDeductionPct = request.LunchDeductionPct;
        rate.DinnerDeductionPct = request.DinnerDeductionPct;
        rate.MileagePerKm = request.MileagePerKm;
        rate.PassengerSurchargePerKm = request.PassengerSurchargePerKm;
        rate.ForestRoadSurchargePerKm = request.ForestRoadSurchargePerKm;
        rate.TrailerSurchargePerKm = request.TrailerSurchargePerKm;
        rate.UndocumentedNightRate = request.UndocumentedNightRate;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
