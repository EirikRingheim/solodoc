using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Expenses;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Expenses;

namespace Solodoc.Api.Endpoints;

public static class TravelExpenseEndpoints
{
    public static WebApplication MapTravelExpenseEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/travel-expenses").RequireAuthorization();

        g.MapGet("/", ListTravelExpenses);
        g.MapGet("/{id:guid}", GetTravelExpense);
        g.MapPost("/", CreateTravelExpense);
        g.MapPost("/calculate", CalculatePreview);
        g.MapDelete("/{id:guid}", DeleteTravelExpense);
        g.MapPatch("/{id:guid}/submit", SubmitTravelExpense);
        g.MapPatch("/{id:guid}/approve", ApproveTravelExpense);
        g.MapPatch("/{id:guid}/reject", RejectTravelExpense);
        g.MapPatch("/{id:guid}/mark-paid", MarkTravelExpensePaid);

        return app;
    }

    private static (Guid? pid, bool valid) GetPerson(ClaimsPrincipal user)
    {
        var c = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(c, out var pid) ? (pid, true) : (null, false);
    }

    private static async Task<bool> IsAdminOrPL(Guid pid, Guid tid, SolodocDbContext db, CancellationToken ct)
    {
        var m = await db.TenantMemberships.FirstOrDefaultAsync(m => m.PersonId == pid && m.TenantId == tid && m.State == TenantMembershipState.Active, ct);
        return m?.Role is TenantRole.TenantAdmin or TenantRole.ProjectLeader;
    }

    private static async Task<bool> IsAdminOrAccountant(Guid pid, Guid tid, SolodocDbContext db, CancellationToken ct)
    {
        var m = await db.TenantMemberships.FirstOrDefaultAsync(m => m.PersonId == pid && m.TenantId == tid && m.State == TenantMembershipState.Active, ct);
        return m?.Role is TenantRole.TenantAdmin or TenantRole.Regnskapsforer;
    }

    // ── Calculate diet, mileage, accommodation ──

    private static async Task<TravelExpenseCalculationDto> Calculate(
        CreateTravelExpenseRequest req, Guid tenantId, SolodocDbContext db, CancellationToken ct)
    {
        var year = req.DepartureDate.Year;
        var rate = await db.TravelExpenseRates
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Year == year && r.IsActive, ct);

        // Fallback to 2026 defaults
        var d6 = rate?.Diet6To12Hours ?? 397m;
        var d12 = rate?.Diet12PlusHours ?? 736m;
        var dNight = rate?.DietOvernight ?? 1012m;
        var bPct = rate?.BreakfastDeductionPct ?? 20m;
        var lPct = rate?.LunchDeductionPct ?? 30m;
        var dPct = rate?.DinnerDeductionPct ?? 50m;
        var kmRate = rate?.MileagePerKm ?? 5.30m;
        var paxRate = rate?.PassengerSurchargePerKm ?? 1.00m;
        var forestRate = rate?.ForestRoadSurchargePerKm ?? 1.00m;
        var trailerRate = rate?.TrailerSurchargePerKm ?? 1.00m;
        var nightRate = rate?.UndocumentedNightRate ?? 452m;

        // Calculate per-day diet
        var days = new List<TravelExpenseDayDto>();
        var totalDays = (req.ReturnDate.ToDateTime(TimeOnly.MinValue) - req.DepartureDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

        foreach (var day in req.Days)
        {
            decimal baseDiet;
            if (day.IsOvernight)
                baseDiet = dNight;
            else if (totalDays == 1)
                baseDiet = d6; // single day trip
            else
                baseDiet = d12;

            // Apply meal deductions
            var deduction = 0m;
            if (day.BreakfastProvided) deduction += baseDiet * (bPct / 100m);
            if (day.LunchProvided) deduction += baseDiet * (lPct / 100m);
            if (day.DinnerProvided) deduction += baseDiet * (dPct / 100m);

            var dayAmount = Math.Max(0, baseDiet - deduction);
            days.Add(new TravelExpenseDayDto(day.Date, day.BreakfastProvided, day.LunchProvided, day.DinnerProvided, day.IsOvernight, Math.Round(dayAmount, 2)));
        }

        var dietTotal = days.Sum(d => d.DietAmount);

        // Mileage
        var mileageTotal = 0m;
        if (req.TotalKm.HasValue && req.TotalKm > 0)
        {
            var ratePerKm = kmRate
                + (req.Passengers * paxRate)
                + (req.ForestRoads ? forestRate : 0)
                + (req.WithTrailer ? trailerRate : 0);
            mileageTotal = Math.Round(req.TotalKm.Value * ratePerKm, 2);
        }

        // Accommodation
        var accommodationTotal = Math.Round(
            (req.NightsUndocumented * nightRate) + req.DocumentedAccommodationCost, 2);

        return new TravelExpenseCalculationDto(
            Math.Round(dietTotal, 2), mileageTotal, accommodationTotal,
            Math.Round(dietTotal + mileageTotal + accommodationTotal, 2),
            days);
    }

    // ── Calculate preview (no save) ──

    private static async Task<IResult> CalculatePreview(
        CreateTravelExpenseRequest request, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var calc = await Calculate(request, tp.TenantId.Value, db, ct);
        return Results.Ok(calc);
    }

    // ── List ──

    private static async Task<IResult> ListTravelExpenses(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        int page = 1, int pageSize = 50, string? status = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var query = db.TravelExpenses.Where(e => e.TenantId == tp.TenantId.Value);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ExpenseStatus>(status, true, out var s))
            query = query.Where(e => e.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.DepartureDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new TravelExpenseListItemDto(
                e.Id,
                db.Persons.Where(p => p.Id == e.PersonId).Select(p => p.FullName).FirstOrDefault() ?? "",
                e.PersonId, e.DepartureDate, e.ReturnDate,
                e.Destination, e.Purpose, e.TotalAmount,
                e.Status.ToString(), e.ApprovedAt.HasValue, e.PaidAt.HasValue,
                e.CreatedAt))
            .ToListAsync(ct);

        return Results.Ok(new { items, totalCount = total, page, pageSize });
    }

    // ── Get ──

    private static async Task<IResult> GetTravelExpense(Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var e = await db.TravelExpenses.Include(t => t.Days)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();

        var name = await db.Persons.Where(p => p.Id == e.PersonId).Select(p => p.FullName).FirstOrDefaultAsync(ct) ?? "";
        var approvedBy = e.ApprovedById.HasValue ? await db.Persons.Where(p => p.Id == e.ApprovedById).Select(p => p.FullName).FirstOrDefaultAsync(ct) : null;
        var paidBy = e.PaidById.HasValue ? await db.Persons.Where(p => p.Id == e.PaidById).Select(p => p.FullName).FirstOrDefaultAsync(ct) : null;

        return Results.Ok(new TravelExpenseDetailDto(
            e.Id, e.PersonId, name, e.DepartureDate, e.ReturnDate,
            e.Destination, e.Purpose, e.TransportMethod.ToString(), e.Route,
            e.TotalKm, e.Passengers, e.ForestRoads, e.WithTrailer,
            e.DietAmount, e.MileageAmount, e.AccommodationAmount, e.TotalAmount,
            e.NightsUndocumented, e.DocumentedAccommodationCost,
            e.Status.ToString(), approvedBy, e.ApprovedAt, paidBy, e.PaidAt, e.RejectionReason,
            e.Days.OrderBy(d => d.Date).Select(d => new TravelExpenseDayDto(
                d.Date, d.BreakfastProvided, d.LunchProvided, d.DinnerProvided, d.IsOvernight, d.DietAmount)).ToList()));
    }

    // ── Create ──

    private static async Task<IResult> CreateTravelExpense(
        CreateTravelExpenseRequest request, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var (pid, valid) = GetPerson(user);
        if (!valid) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Destination))
            return Results.BadRequest(new { error = "Reisemål er påkrevd." });

        // Calculate amounts
        var calc = await Calculate(request, tp.TenantId.Value, db, ct);

        if (!Enum.TryParse<TransportMethod>(request.TransportMethod, true, out var transport))
            transport = TransportMethod.Bil;

        var te = new TravelExpense
        {
            TenantId = tp.TenantId.Value,
            PersonId = pid!.Value,
            ProjectId = request.ProjectId,
            DepartureDate = request.DepartureDate,
            ReturnDate = request.ReturnDate,
            Destination = request.Destination,
            Purpose = request.Purpose,
            TransportMethod = transport,
            Route = request.Route,
            TotalKm = request.TotalKm,
            Passengers = request.Passengers,
            ForestRoads = request.ForestRoads,
            WithTrailer = request.WithTrailer,
            DietAmount = calc.DietTotal,
            MileageAmount = calc.MileageTotal,
            AccommodationAmount = calc.AccommodationTotal,
            TotalAmount = calc.GrandTotal,
            NightsUndocumented = request.NightsUndocumented,
            DocumentedAccommodationCost = request.DocumentedAccommodationCost,
            Status = ExpenseStatus.Draft
        };

        db.TravelExpenses.Add(te);

        // Add days
        foreach (var day in calc.DayBreakdown)
        {
            db.TravelExpenseDays.Add(new TravelExpenseDay
            {
                TravelExpenseId = te.Id,
                Date = day.Date,
                BreakfastProvided = day.BreakfastProvided,
                LunchProvided = day.LunchProvided,
                DinnerProvided = day.DinnerProvided,
                IsOvernight = day.IsOvernight,
                DietAmount = day.DietAmount
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/travel-expenses/{te.Id}", new { id = te.Id });
    }

    // ── Delete ──

    private static async Task<IResult> DeleteTravelExpense(
        Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var e = await db.TravelExpenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status != ExpenseStatus.Draft)
            return Results.BadRequest(new { error = "Kan kun slette utkast." });

        e.IsDeleted = true;
        e.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Status transitions ──

    private static async Task<IResult> SubmitTravelExpense(Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var e = await db.TravelExpenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status is not ExpenseStatus.Draft and not ExpenseStatus.Rejected)
            return Results.BadRequest(new { error = "Kan kun sende inn utkast." });
        e.Status = ExpenseStatus.Submitted;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ApproveTravelExpense(
        Guid id, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var (pid, valid) = GetPerson(user);
        if (!valid) return Results.Unauthorized();
        if (!await IsAdminOrPL(pid!.Value, tp.TenantId.Value, db, ct)) return Results.Forbid();

        var e = await db.TravelExpenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status != ExpenseStatus.Submitted) return Results.BadRequest(new { error = "Kan kun godkjenne innsendte." });

        e.Status = ExpenseStatus.Approved;
        e.ApprovedById = pid;
        e.ApprovedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> RejectTravelExpense(
        Guid id, RejectExpenseRequest request, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var (pid, valid) = GetPerson(user);
        if (!valid) return Results.Unauthorized();
        if (!await IsAdminOrPL(pid!.Value, tp.TenantId.Value, db, ct)) return Results.Forbid();

        var e = await db.TravelExpenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status != ExpenseStatus.Submitted) return Results.BadRequest(new { error = "Kan kun avvise innsendte." });

        e.Status = ExpenseStatus.Rejected;
        e.RejectionReason = request.Reason;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> MarkTravelExpensePaid(
        Guid id, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var (pid, valid) = GetPerson(user);
        if (!valid) return Results.Unauthorized();
        if (!await IsAdminOrAccountant(pid!.Value, tp.TenantId.Value, db, ct)) return Results.Forbid();

        var e = await db.TravelExpenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status != ExpenseStatus.Approved) return Results.BadRequest(new { error = "Kan kun markere godkjente som betalt." });

        e.Status = ExpenseStatus.Paid;
        e.PaidById = pid;
        e.PaidAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
