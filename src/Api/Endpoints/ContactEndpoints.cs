using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Contacts;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Contacts;

namespace Solodoc.Api.Endpoints;

public static class ContactEndpoints
{
    public static WebApplication MapContactEndpoints(this WebApplication app)
    {
        app.MapGet("/api/contacts", ListContacts).RequireAuthorization();
        app.MapPost("/api/contacts", CreateContact).RequireAuthorization();
        app.MapGet("/api/contacts/{id:guid}", GetContact).RequireAuthorization();
        app.MapPut("/api/contacts/{id:guid}", UpdateContact).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListContacts(
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct,
        string? search = null)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var query = db.Contacts
            .Where(c => c.TenantId == tenantProvider.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Title != null && c.Title.ToLower().Contains(term)) ||
                (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        var items = await query
            .OrderBy(c => c.Name)
            .Select(c => new ContactListItemDto(
                c.Id,
                c.Name,
                c.Type.ToString(),
                c.Phone,
                c.Email,
                c.City))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateContact(
        CreateContactRequest request,
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
            "Kunde" => ContactType.Kunde,
            "Underleverandor" or "Underleverandør" => ContactType.Underleverandor,
            "Leverandor" or "Leverandør" => ContactType.Leverandor,
            "Inspektor" or "Inspektør" => ContactType.Inspektor,
            "Radgiver" or "Rådgiver" => ContactType.Radgiver,
            "Annet" => ContactType.Annet,
            _ => ContactType.Annet
        };

        var contact = new Contact
        {
            TenantId = tenantProvider.TenantId.Value,
            Name = request.Name,
            Type = type,
            OrgNumber = request.OrgNumber,
            Address = request.Address,
            PostalCode = request.PostalCode,
            City = request.City,
            Phone = request.Phone,
            Email = request.Email,
            Title = request.Title,
            Description = request.Description,
            Notes = request.Notes
        };

        db.Contacts.Add(contact);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/contacts/{contact.Id}", new { id = contact.Id });
    }

    private static async Task<IResult> GetContact(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var contact = await db.Contacts
            .Where(c => c.Id == id && c.TenantId == tenantProvider.TenantId.Value)
            .Select(c => new ContactDetailDto(
                c.Id,
                c.Name,
                c.Type.ToString(),
                c.OrgNumber,
                c.Address,
                c.PostalCode,
                c.City,
                c.Phone,
                c.Email,
                c.Title,
                c.Description,
                c.Notes))
            .FirstOrDefaultAsync(ct);

        return contact is not null ? Results.Ok(contact) : Results.NotFound();
    }

    private static async Task<IResult> UpdateContact(
        Guid id,
        CreateContactRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantProvider.TenantId.Value, ct);
        if (contact is null)
            return Results.NotFound();

        var type = request.Type switch
        {
            "Kunde" => ContactType.Kunde,
            "Underleverandor" or "Underleverandør" => ContactType.Underleverandor,
            "Leverandor" or "Leverandør" => ContactType.Leverandor,
            "Inspektor" or "Inspektør" => ContactType.Inspektor,
            "Radgiver" or "Rådgiver" => ContactType.Radgiver,
            "Annet" => ContactType.Annet,
            _ => ContactType.Annet
        };

        contact.Name = request.Name;
        contact.Type = type;
        contact.OrgNumber = request.OrgNumber;
        contact.Address = request.Address;
        contact.PostalCode = request.PostalCode;
        contact.City = request.City;
        contact.Phone = request.Phone;
        contact.Email = request.Email;
        contact.Title = request.Title;
        contact.Description = request.Description;
        contact.Notes = request.Notes;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
