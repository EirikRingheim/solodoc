using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Solodoc.Application.Auth;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Entities.Contacts;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Auth;
using Solodoc.Shared.Contacts;

namespace Solodoc.IntegrationTests;

public class ContactEndpointTests : IAsyncLifetime
{
    private readonly SolodocApiFactory _factory;
    private readonly HttpClient _client;
    private Guid _tenantId;

    public ContactEndpointTests()
    {
        _factory = new SolodocApiFactory();
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SolodocDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var tenant = new Tenant { Name = "Test Tenant", OrgNumber = "999999999", BusinessType = BusinessType.AS };
        db.Tenants.Add(tenant);
        _tenantId = tenant.Id;

        var admin = new Person
        {
            Email = "admin@test.dev",
            FullName = "Admin User",
            PasswordHash = hasher.Hash("TestPass1"),
            State = PersonState.Active,
            EmailVerified = true
        };
        db.Persons.Add(admin);

        db.TenantMemberships.Add(new TenantMembership
        {
            PersonId = admin.Id,
            TenantId = tenant.Id,
            Role = TenantRole.TenantAdmin,
            State = TenantMembershipState.Active
        });

        // Seed contacts for list/search tests
        db.Contacts.AddRange(
            new Contact
            {
                TenantId = tenant.Id,
                Name = "Fjellbygg Entreprenør AS",
                Type = ContactType.Kunde,
                OrgNumber = "912345678",
                City = "Bergen",
                Phone = "55123456"
            },
            new Contact
            {
                TenantId = tenant.Id,
                Name = "Vestland Rør og Varme",
                Type = ContactType.Underleverandor,
                City = "Oslo",
                Phone = "22123456"
            }
        );

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task Authenticate()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin@test.dev", "TestPass1"));
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task CreateContact_ValidData_ReturnsCreated()
    {
        await Authenticate();

        var response = await _client.PostAsJsonAsync("/api/contacts",
            new CreateContactRequest("Ny Kunde AS", "Kunde", "987654321", "Storgata 1", "5003", "Bergen", "55998877", "post@nykunde.no", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetContact_Exists_ReturnsDetail()
    {
        await Authenticate();

        // Create a contact first
        var createResponse = await _client.PostAsJsonAsync("/api/contacts",
            new CreateContactRequest("Detail Kontakt", "Leverandør", null, "Industriveien 5", "4020", "Stavanger", null, "kontakt@detail.no", null, "Rørleverandør", null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.GetAsync($"/api/contacts/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<ContactDetailDto>();
        detail.Should().NotBeNull();
        detail!.Name.Should().Be("Detail Kontakt");
        detail.City.Should().Be("Stavanger");
        detail.Email.Should().Be("kontakt@detail.no");
    }

    [Fact]
    public async Task ListContacts_WithSearch_FiltersResults()
    {
        await Authenticate();

        var response = await _client.GetFromJsonAsync<List<ContactListItemDto>>("/api/contacts?search=Fjellbygg");

        response.Should().NotBeNull();
        response!.Should().ContainSingle(c => c.Name.Contains("Fjellbygg"));
    }

    private record IdResponse(Guid Id);
}
