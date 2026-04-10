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
using Solodoc.Shared.Hours;

namespace Solodoc.IntegrationTests;

public class HoursEndpointTests : IAsyncLifetime
{
    private readonly SolodocApiFactory _factory;
    private readonly HttpClient _client;
    private Guid _tenantId;
    private Guid _adminPersonId;

    public HoursEndpointTests()
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
        _adminPersonId = admin.Id;

        db.TenantMemberships.Add(new TenantMembership
        {
            PersonId = admin.Id,
            TenantId = tenant.Id,
            Role = TenantRole.TenantAdmin,
            State = TenantMembershipState.Active
        });

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
    public async Task CreateManualEntry_ValidData_ReturnsCreated()
    {
        await Authenticate();

        var response = await _client.PostAsJsonAsync("/api/hours",
            new ManualTimeEntryRequest(DateOnly.FromDateTime(DateTime.UtcNow), 7.5m, null, null, "Arbeid", "Kontorarbeid"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ClockIn_NoActiveClock_Succeeds()
    {
        await Authenticate();

        var response = await _client.PostAsJsonAsync("/api/hours/clock-in",
            new ClockInRequest(null, null, "Arbeid", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ClockIn_AlreadyClockedIn_ReturnsBadRequest()
    {
        await Authenticate();

        // First clock-in
        await _client.PostAsJsonAsync("/api/hours/clock-in",
            new ClockInRequest(null, null, "Arbeid", null, null));

        // Second clock-in should fail
        var response = await _client.PostAsJsonAsync("/api/hours/clock-in",
            new ClockInRequest(null, null, "Arbeid", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ClockOut_ActiveClock_ReturnsEntry()
    {
        await Authenticate();

        // Clock in first
        await _client.PostAsJsonAsync("/api/hours/clock-in",
            new ClockInRequest(null, null, "Arbeid", null, null));

        // Clock out
        var response = await _client.PostAsJsonAsync("/api/hours/clock-out",
            new ClockOutRequest(null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = await response.Content.ReadFromJsonAsync<TimeEntryDetailDto>();
        entry.Should().NotBeNull();
        entry!.Hours.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SubmitEntry_DraftEntry_ChangesStatus()
    {
        await Authenticate();

        // Create manual entry (starts as Draft)
        var createResponse = await _client.PostAsJsonAsync("/api/hours",
            new ManualTimeEntryRequest(DateOnly.FromDateTime(DateTime.UtcNow), 8m, null, null, "Arbeid", null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        // Submit
        var response = await _client.PatchAsync($"/api/hours/{created!.Id}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StatusResponse>();
        body!.Status.Should().Be("Innsendt");
    }

    [Fact]
    public async Task ApproveEntry_SubmittedEntry_ChangesStatus()
    {
        await Authenticate();

        // Create and submit a manual entry
        var createResponse = await _client.PostAsJsonAsync("/api/hours",
            new ManualTimeEntryRequest(DateOnly.FromDateTime(DateTime.UtcNow), 8m, null, null, "Arbeid", null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        await _client.PatchAsync($"/api/hours/{created!.Id}/submit", null);

        // Approve (admin can approve)
        var response = await _client.PatchAsync($"/api/hours/{created.Id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StatusResponse>();
        body!.Status.Should().Be("Godkjent");
    }

    private record IdResponse(Guid Id);
    private record StatusResponse(string Status);
}
