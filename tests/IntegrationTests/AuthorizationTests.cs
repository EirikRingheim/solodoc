using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Solodoc.Application.Auth;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Auth;
using Solodoc.Shared.Deviations;

namespace Solodoc.IntegrationTests;

public class AuthorizationTests : IAsyncLifetime
{
    private readonly SolodocApiFactory _factory;
    private readonly HttpClient _client;
    private Guid _tenantId;

    public AuthorizationTests()
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

        // Admin user
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

        // Field worker user
        var worker = new Person
        {
            Email = "worker@test.dev",
            FullName = "Field Worker",
            PasswordHash = hasher.Hash("TestPass1"),
            State = PersonState.Active,
            EmailVerified = true
        };
        db.Persons.Add(worker);

        db.TenantMemberships.Add(new TenantMembership
        {
            PersonId = worker.Id,
            TenantId = tenant.Id,
            Role = TenantRole.FieldWorker,
            State = TenantMembershipState.Active
        });

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<string> LoginAs(string email)
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "TestPass1"));
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    [Fact]
    public async Task Unauthenticated_GetProjects_ReturnsUnauthorized()
    {
        // No token set — should get 401
        var response = await _client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FieldWorker_CannotAccessAdminHours()
    {
        var token = await LoginAs("worker@test.dev");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/hours/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FieldWorker_CanCreateDeviation()
    {
        var token = await LoginAs("worker@test.dev");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/deviations",
            new CreateDeviationRequest("Ny avvik fra arbeider", "Beskrivelse", "Lav", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
