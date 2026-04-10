using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Projects;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Common;
using Solodoc.Shared.Projects;

namespace Solodoc.Api.Endpoints;

public static class CustomerEndpoints
{
    public static WebApplication MapCustomerEndpoints(this WebApplication app)
    {
        app.MapGet("/api/customers", ListCustomers).RequireAuthorization();
        app.MapPost("/api/customers", CreateCustomer).RequireAuthorization();
        app.MapPut("/api/customers/{id:guid}", UpdateCustomer).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListCustomers(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct,
        int page = 1,
        int pageSize = 10,
        string? search = null)
    {
        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId))
            return Results.Unauthorized();

        var membership = await db.TenantMemberships
            .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active)
            .FirstOrDefaultAsync(ct);

        if (membership is null)
            return Results.Ok(new PagedResult<CustomerDto>([], 0, page, pageSize));

        var tenantId = membership.TenantId;

        var query = db.Customers.Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.OrgNumber != null && c.OrgNumber.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerDto(
                c.Id,
                c.Name,
                c.Type == CustomerType.Bedrift ? "Bedrift" : "Privatperson",
                c.OrgNumber,
                c.Address,
                c.City,
                c.Phone,
                c.Email))
            .ToListAsync(ct);

        return Results.Ok(new PagedResult<CustomerDto>(items, totalCount, page, pageSize));
    }

    private static async Task<IResult> CreateCustomer(
        CreateCustomerRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var type = request.Type switch
        {
            "Bedrift" => CustomerType.Bedrift,
            "Privatperson" => CustomerType.Privatperson,
            _ => (CustomerType?)null
        };

        if (type is null)
            return Results.BadRequest(new { error = $"Ugyldig type: {request.Type}. Bruk 'Bedrift' eller 'Privatperson'." });

        var customer = new Customer
        {
            TenantId = tenantProvider.TenantId.Value,
            Name = request.Name,
            Type = type.Value,
            OrgNumber = request.OrgNumber,
            Address = request.Address,
            PostalCode = request.PostalCode,
            City = request.City,
            Phone = request.Phone,
            Email = request.Email,
            Notes = request.Notes
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/customers/{customer.Id}", new { id = customer.Id });
    }

    private static async Task<IResult> UpdateCustomer(
        Guid id,
        UpdateCustomerRequest request,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
            return Results.NotFound();

        var type = request.Type switch
        {
            "Bedrift" => CustomerType.Bedrift,
            "Privatperson" => CustomerType.Privatperson,
            _ => (CustomerType?)null
        };

        if (type is null)
            return Results.BadRequest(new { error = $"Ugyldig type: {request.Type}. Bruk 'Bedrift' eller 'Privatperson'." });

        customer.Name = request.Name;
        customer.Type = type.Value;
        customer.OrgNumber = request.OrgNumber;
        customer.Address = request.Address;
        customer.PostalCode = request.PostalCode;
        customer.City = request.City;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Notes = request.Notes;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
