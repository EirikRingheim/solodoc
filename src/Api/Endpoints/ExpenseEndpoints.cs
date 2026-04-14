using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Expenses;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Expenses;

namespace Solodoc.Api.Endpoints;

public static class ExpenseEndpoints
{
    public static WebApplication MapExpenseEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/expenses").RequireAuthorization();

        g.MapGet("/", ListExpenses);
        g.MapGet("/{id:guid}", GetExpense);
        g.MapPost("/", CreateExpense);
        g.MapPut("/{id:guid}", UpdateExpense);
        g.MapDelete("/{id:guid}", DeleteExpense);
        g.MapPatch("/{id:guid}/submit", SubmitExpense);
        g.MapPatch("/{id:guid}/approve", ApproveExpense);
        g.MapPatch("/{id:guid}/reject", RejectExpense);
        g.MapPatch("/{id:guid}/mark-paid", MarkExpensePaid);

        return app;
    }

    private static (Guid? personId, bool valid) GetPerson(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var pid) ? (pid, true) : (null, false);
    }

    private static async Task<bool> IsAdminOrPL(Guid personId, Guid tenantId, SolodocDbContext db, CancellationToken ct)
    {
        var m = await db.TenantMemberships.FirstOrDefaultAsync(
            m => m.PersonId == personId && m.TenantId == tenantId && m.State == TenantMembershipState.Active, ct);
        return m?.Role is TenantRole.TenantAdmin or TenantRole.ProjectLeader;
    }

    private static async Task<bool> IsAdminOrAccountant(Guid personId, Guid tenantId, SolodocDbContext db, CancellationToken ct)
    {
        var m = await db.TenantMemberships.FirstOrDefaultAsync(
            m => m.PersonId == personId && m.TenantId == tenantId && m.State == TenantMembershipState.Active, ct);
        return m?.Role is TenantRole.TenantAdmin or TenantRole.Regnskapsforer;
    }

    // ── List ──

    private static async Task<IResult> ListExpenses(
        ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        int page = 1, int pageSize = 50, string? status = null, Guid? personId = null,
        Guid? projectId = null, string? from = null, string? to = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;

        var query = db.Expenses.Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ExpenseStatus>(status, true, out var s))
            query = query.Where(e => e.Status == s);
        if (personId.HasValue)
            query = query.Where(e => e.PersonId == personId.Value);
        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);
        if (DateOnly.TryParse(from, out var fromDate))
            query = query.Where(e => e.Date >= fromDate);
        if (DateOnly.TryParse(to, out var toDate))
            query = query.Where(e => e.Date <= toDate);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new ExpenseListItemDto(
                e.Id,
                db.Persons.Where(p => p.Id == e.PersonId).Select(p => p.FullName).FirstOrDefault() ?? "",
                e.PersonId, e.Date, e.Amount,
                e.Category.HasValue ? e.Category.Value.ToString() : null,
                e.Description,
                e.ProjectId.HasValue ? db.Projects.Where(p => p.Id == e.ProjectId).Select(p => p.Name).FirstOrDefault() : null,
                e.ProjectId, e.ReceiptFileKey, e.Status.ToString(),
                e.ApprovedAt.HasValue, e.PaidAt.HasValue,
                e.ApprovedById.HasValue ? db.Persons.Where(p => p.Id == e.ApprovedById).Select(p => p.FullName).FirstOrDefault() : null,
                e.PaidById.HasValue ? db.Persons.Where(p => p.Id == e.PaidById).Select(p => p.FullName).FirstOrDefault() : null,
                e.CreatedAt))
            .ToListAsync(ct);

        return Results.Ok(new { items, totalCount = total, page, pageSize });
    }

    // ── Get ──

    private static async Task<IResult> GetExpense(Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var e = await db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();

        var personName = await db.Persons.Where(p => p.Id == e.PersonId).Select(p => p.FullName).FirstOrDefaultAsync(ct) ?? "";
        var projectName = e.ProjectId.HasValue ? await db.Projects.Where(p => p.Id == e.ProjectId).Select(p => p.Name).FirstOrDefaultAsync(ct) : null;
        var jobDesc = e.JobId.HasValue ? await db.Jobs.Where(j => j.Id == e.JobId).Select(j => j.Description).FirstOrDefaultAsync(ct) : null;
        var approvedBy = e.ApprovedById.HasValue ? await db.Persons.Where(p => p.Id == e.ApprovedById).Select(p => p.FullName).FirstOrDefaultAsync(ct) : null;
        var paidBy = e.PaidById.HasValue ? await db.Persons.Where(p => p.Id == e.PaidById).Select(p => p.FullName).FirstOrDefaultAsync(ct) : null;

        return Results.Ok(new ExpenseDetailDto(
            e.Id, e.PersonId, personName, e.Date, e.Amount,
            e.Category?.ToString(), e.Description,
            projectName, e.ProjectId, jobDesc, e.JobId,
            e.ReceiptFileKey, e.Status.ToString(),
            approvedBy, e.ApprovedAt, paidBy, e.PaidAt, e.RejectionReason));
    }

    // ── Create ──

    private static async Task<IResult> CreateExpense(
        CreateExpenseRequest request, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var (pid, valid) = GetPerson(user);
        if (!valid) return Results.Unauthorized();

        if (request.Amount <= 0)
            return Results.BadRequest(new { error = "Beløp må være større enn 0." });
        if (string.IsNullOrWhiteSpace(request.ReceiptFileKey))
            return Results.BadRequest(new { error = "Kvitteringsbilde er påkrevd." });

        // Validate required fields per tenant settings
        var settings = await db.ExpenseSettingsTable.FirstOrDefaultAsync(s => s.TenantId == tp.TenantId.Value, ct);
        if (settings is not null)
        {
            if (settings.RequireDate && !request.Date.HasValue)
                return Results.BadRequest(new { error = "Dato er påkrevd." });
            if (settings.RequireDescription && string.IsNullOrWhiteSpace(request.Description))
                return Results.BadRequest(new { error = "Beskrivelse er påkrevd." });
            if (settings.RequireCategory && string.IsNullOrWhiteSpace(request.Category))
                return Results.BadRequest(new { error = "Kategori er påkrevd." });
            if (settings.RequireProject && !request.ProjectId.HasValue && !request.JobId.HasValue)
                return Results.BadRequest(new { error = "Prosjekt eller oppdrag er påkrevd." });
        }

        ExpenseCategory? cat = null;
        if (!string.IsNullOrWhiteSpace(request.Category) && Enum.TryParse<ExpenseCategory>(request.Category, true, out var parsed))
            cat = parsed;

        var expense = new Expense
        {
            TenantId = tp.TenantId.Value,
            PersonId = pid!.Value,
            Date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = request.Amount,
            Category = cat,
            Description = request.Description,
            ProjectId = request.ProjectId,
            JobId = request.JobId,
            ReceiptFileKey = request.ReceiptFileKey,
            Status = ExpenseStatus.Draft
        };

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/expenses/{expense.Id}", new { id = expense.Id });
    }

    // ── Update (draft only) ──

    private static async Task<IResult> UpdateExpense(
        Guid id, UpdateExpenseSettingsRequest request, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var e = await db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status is not ExpenseStatus.Draft and not ExpenseStatus.Rejected)
            return Results.BadRequest(new { error = "Kan kun redigere utkast eller avviste utlegg." });

        // Apply updates from whatever fields are provided
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ── Delete (draft only) ──

    private static async Task<IResult> DeleteExpense(
        Guid id, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var e = await db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status != ExpenseStatus.Draft)
            return Results.BadRequest(new { error = "Kan kun slette utkast." });

        e.IsDeleted = true;
        e.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Submit ──

    private static async Task<IResult> SubmitExpense(Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var e = await db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status is not ExpenseStatus.Draft and not ExpenseStatus.Rejected)
            return Results.BadRequest(new { error = "Kan kun sende inn utkast." });

        e.Status = ExpenseStatus.Submitted;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ── Approve (admin/PL) ──

    private static async Task<IResult> ApproveExpense(
        Guid id, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var (pid, valid) = GetPerson(user);
        if (!valid) return Results.Unauthorized();
        if (!await IsAdminOrPL(pid!.Value, tp.TenantId.Value, db, ct)) return Results.Forbid();

        var e = await db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status != ExpenseStatus.Submitted)
            return Results.BadRequest(new { error = "Kan kun godkjenne innsendte utlegg." });

        e.Status = ExpenseStatus.Approved;
        e.ApprovedById = pid;
        e.ApprovedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ── Reject (admin/PL) ──

    private static async Task<IResult> RejectExpense(
        Guid id, RejectExpenseRequest request, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var (pid, valid) = GetPerson(user);
        if (!valid) return Results.Unauthorized();
        if (!await IsAdminOrPL(pid!.Value, tp.TenantId.Value, db, ct)) return Results.Forbid();

        var e = await db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status != ExpenseStatus.Submitted)
            return Results.BadRequest(new { error = "Kan kun avvise innsendte utlegg." });

        e.Status = ExpenseStatus.Rejected;
        e.RejectionReason = request.Reason;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ── Mark Paid (admin or accountant) ──

    private static async Task<IResult> MarkExpensePaid(
        Guid id, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var (pid, valid) = GetPerson(user);
        if (!valid) return Results.Unauthorized();
        if (!await IsAdminOrAccountant(pid!.Value, tp.TenantId.Value, db, ct)) return Results.Forbid();

        var e = await db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tp.TenantId.Value, ct);
        if (e is null) return Results.NotFound();
        if (e.Status != ExpenseStatus.Approved)
            return Results.BadRequest(new { error = "Kan kun markere godkjente utlegg som betalt." });

        e.Status = ExpenseStatus.Paid;
        e.PaidById = pid;
        e.PaidAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
