using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Solodoc.Application.Auth;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Entities.Deviations;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Auth;
using Solodoc.Shared.Common;
using Solodoc.Shared.Deviations;

namespace Solodoc.IntegrationTests;

public class DeviationEndpointTests : IAsyncLifetime
{
    private readonly SolodocApiFactory _factory;
    private readonly HttpClient _client;
    private Guid _tenantId;

    public DeviationEndpointTests()
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

        // Seed existing deviations for count/list tests
        db.Deviations.AddRange(
            new Deviation
            {
                TenantId = tenant.Id, Title = "Avvik 1", Status = DeviationStatus.Open,
                Severity = DeviationSeverity.Medium, ReportedById = admin.Id
            },
            new Deviation
            {
                TenantId = tenant.Id, Title = "Avvik 2", Status = DeviationStatus.Open,
                Severity = DeviationSeverity.High, ReportedById = admin.Id
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
    public async Task CreateDeviation_ValidData_ReturnsCreated()
    {
        await Authenticate();

        var response = await _client.PostAsJsonAsync("/api/deviations",
            new CreateDeviationRequest("Ny avvik", "Beskrivelse av avviket", "Høy", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDeviation_Exists_ReturnsDetail()
    {
        await Authenticate();

        // Create deviation
        var createResponse = await _client.PostAsJsonAsync("/api/deviations",
            new CreateDeviationRequest("Detail avvik", "For detaljvisning", "Middels", null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.GetAsync($"/api/deviations/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<DeviationDetailDto>();
        detail.Should().NotBeNull();
        detail!.Title.Should().Be("Detail avvik");
        detail.Status.Should().Be("Åpen");
        detail.Severity.Should().Be("Middels");
    }

    [Fact]
    public async Task UpdateStatus_ToInProgress_Succeeds()
    {
        await Authenticate();

        // Create deviation
        var createResponse = await _client.PostAsJsonAsync("/api/deviations",
            new CreateDeviationRequest("Status test", null, "Lav", null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.PutAsJsonAsync($"/api/deviations/{created!.Id}/status",
            new UpdateDeviationStatusRequest("Under behandling"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status changed
        var detail = await _client.GetFromJsonAsync<DeviationDetailDto>($"/api/deviations/{created.Id}");
        detail!.Status.Should().Be("Under behandling");
    }

    [Fact]
    public async Task UpdateStatus_ToClosed_SetsClosedAt()
    {
        await Authenticate();

        // Create deviation
        var createResponse = await _client.PostAsJsonAsync("/api/deviations",
            new CreateDeviationRequest("Lukk test", null, "Middels", null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.PutAsJsonAsync($"/api/deviations/{created!.Id}/status",
            new UpdateDeviationStatusRequest("Lukket"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify ClosedAt is set
        var detail = await _client.GetFromJsonAsync<DeviationDetailDto>($"/api/deviations/{created.Id}");
        detail!.Status.Should().Be("Lukket");
        detail.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOpenCount_ReturnsCorrectCount()
    {
        await Authenticate();

        var response = await _client.GetAsync("/api/deviations/open-count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CountResponse>();
        body!.Count.Should().BeGreaterThanOrEqualTo(2, "we seeded 2 open deviations");
    }

    private record IdResponse(Guid Id);
    private record CountResponse(int Count);
}
