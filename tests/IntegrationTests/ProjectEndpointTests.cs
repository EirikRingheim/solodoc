using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Solodoc.Application.Auth;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Entities.Projects;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Auth;
using Solodoc.Shared.Common;
using Solodoc.Shared.Projects;

namespace Solodoc.IntegrationTests;

public class ProjectEndpointTests : IAsyncLifetime
{
    private readonly SolodocApiFactory _factory;
    private readonly HttpClient _client;
    private Guid _tenantId;
    private Guid _adminPersonId;

    public ProjectEndpointTests()
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

        // Seed some projects for list/search tests
        db.Projects.AddRange(
            new Project { TenantId = tenant.Id, Name = "Fjellbygg Renovering", Status = ProjectStatus.Active, ClientName = "Ola Nordmann" },
            new Project { TenantId = tenant.Id, Name = "Vestland Kontor", Status = ProjectStatus.Active, ClientName = "Kari Hansen" }
        );

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

    private async Task Authenticate()
    {
        var token = await LoginAs("admin@test.dev");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task CreateProject_ValidData_ReturnsCreated()
    {
        await Authenticate();

        var response = await _client.PostAsJsonAsync("/api/projects",
            new CreateProjectRequest("Nytt Prosjekt", "Beskrivelse", null, "Test Kunde", "Oslo", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProject_Exists_ReturnsDetail()
    {
        await Authenticate();

        // Create a project first
        var createResponse = await _client.PostAsJsonAsync("/api/projects",
            new CreateProjectRequest("Detail Project", "Detaljer", null, "Kunde AS", "Bergen", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.GetAsync($"/api/projects/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var project = await response.Content.ReadFromJsonAsync<ProjectDetailDto>();
        project.Should().NotBeNull();
        project!.Name.Should().Be("Detail Project");
        project.ClientName.Should().Be("Kunde AS");
    }

    [Fact]
    public async Task ListProjects_WithSearch_FiltersResults()
    {
        await Authenticate();

        var response = await _client.GetFromJsonAsync<PagedResult<ProjectListItemDto>>("/api/projects?search=Fjellbygg");

        response.Should().NotBeNull();
        response!.TotalCount.Should().Be(1);
        response.Items.Should().ContainSingle(p => p.Name.Contains("Fjellbygg"));
    }

    [Fact]
    public async Task UpdateProjectStatus_ToActive_Succeeds()
    {
        await Authenticate();

        // Create a project (starts as Planlagt)
        var createResponse = await _client.PostAsJsonAsync("/api/projects",
            new CreateProjectRequest("Status Project", null, null, null, null, null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var response = await _client.PatchAsJsonAsync($"/api/projects/{created!.Id}/status",
            new ChangeStatusRequest("Aktiv"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the status changed
        var detail = await _client.GetFromJsonAsync<ProjectDetailDto>($"/api/projects/{created.Id}");
        detail!.Status.Should().Be("Aktiv");
    }

    [Fact]
    public async Task DeleteProject_SoftDeletes()
    {
        await Authenticate();

        // Create a project
        var createResponse = await _client.PostAsJsonAsync("/api/projects",
            new CreateProjectRequest("Delete Me", null, null, null, null, null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/projects/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // GET should return 404 (soft-deleted, filtered out)
        var getResponse = await _client.GetAsync($"/api/projects/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record IdResponse(Guid Id);
}
