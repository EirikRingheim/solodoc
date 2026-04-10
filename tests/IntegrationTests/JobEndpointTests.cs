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
using Solodoc.Shared.Common;
using Solodoc.Shared.Projects;

namespace Solodoc.IntegrationTests;

public class JobEndpointTests : IAsyncLifetime
{
    private readonly SolodocApiFactory _factory;
    private readonly HttpClient _client;
    private Guid _tenantId;

    public JobEndpointTests()
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
    public async Task CreateJob_ValidData_ReturnsCreated()
    {
        await Authenticate();

        var response = await _client.PostAsJsonAsync("/api/jobs",
            new CreateJobRequest("Reparasjon av vannlekkasje", null, "Storgata 5, Bergen", "Haster"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetJob_Exists_ReturnsDetail()
    {
        await Authenticate();

        var createResponse = await _client.PostAsJsonAsync("/api/jobs",
            new CreateJobRequest("Service hos kunde", null, "Parkveien 10", "Planlagt besøk"));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.GetAsync($"/api/jobs/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var job = await response.Content.ReadFromJsonAsync<JobDetailDto>();
        job.Should().NotBeNull();
        job!.Description.Should().Be("Service hos kunde");
        job.Status.Should().Be("Aktiv");
        job.Address.Should().Be("Parkveien 10");
    }

    [Fact]
    public async Task AddPartsItem_AddsToJob()
    {
        await Authenticate();

        // Create a job first
        var createResponse = await _client.PostAsJsonAsync("/api/jobs",
            new CreateJobRequest("Oppdrag med deler", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        // Add a parts item
        var partsResponse = await _client.PostAsJsonAsync($"/api/jobs/{created!.Id}/parts",
            new AddPartsItemRequest("Ventil DN50", "Bestilles fra Ahlsell"));

        partsResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify the parts item is on the job
        var job = await _client.GetFromJsonAsync<JobDetailDto>($"/api/jobs/{created.Id}");
        job!.PartsItems.Should().ContainSingle(p => p.Description == "Ventil DN50");
    }

    [Fact]
    public async Task UpdateStatus_ToCompleted_Succeeds()
    {
        await Authenticate();

        var createResponse = await _client.PostAsJsonAsync("/api/jobs",
            new CreateJobRequest("Fullfør dette", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.PatchAsJsonAsync($"/api/jobs/{created!.Id}/status",
            new ChangeStatusRequest("Fullført"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status
        var job = await _client.GetFromJsonAsync<JobDetailDto>($"/api/jobs/{created.Id}");
        job!.Status.Should().Be("Fullført");
    }

    private record IdResponse(Guid Id);
}
