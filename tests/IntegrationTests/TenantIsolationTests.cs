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

public class TenantIsolationTests : IAsyncLifetime
{
    private readonly SolodocApiFactory _factory;
    private readonly HttpClient _client;

    public TenantIsolationTests()
    {
        _factory = new SolodocApiFactory();
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SolodocDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var tenantA = new Tenant { Name = "Tenant A", OrgNumber = "111111111", BusinessType = BusinessType.AS };
        var tenantB = new Tenant { Name = "Tenant B", OrgNumber = "222222222", BusinessType = BusinessType.AS };
        db.Tenants.AddRange(tenantA, tenantB);

        var userA = new Person
        {
            Email = "usera@test.dev", FullName = "User A",
            PasswordHash = hasher.Hash("TestPass1"), State = PersonState.Active, EmailVerified = true
        };
        var userB = new Person
        {
            Email = "userb@test.dev", FullName = "User B",
            PasswordHash = hasher.Hash("TestPass1"), State = PersonState.Active, EmailVerified = true
        };
        db.Persons.AddRange(userA, userB);

        db.TenantMemberships.AddRange(
            new TenantMembership { PersonId = userA.Id, TenantId = tenantA.Id, Role = TenantRole.TenantAdmin },
            new TenantMembership { PersonId = userB.Id, TenantId = tenantB.Id, Role = TenantRole.TenantAdmin }
        );

        db.Projects.AddRange(
            new Project { TenantId = tenantA.Id, Name = "A-Project-1", Status = ProjectStatus.Active },
            new Project { TenantId = tenantA.Id, Name = "A-Project-2", Status = ProjectStatus.Active },
            new Project { TenantId = tenantB.Id, Name = "B-Project-1", Status = ProjectStatus.Active }
        );

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task TenantA_OnlySees_OwnProjects()
    {
        var token = await LoginAs("usera@test.dev");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetFromJsonAsync<PagedResult<ProjectListItemDto>>("/api/projects");

        response.Should().NotBeNull();
        response!.TotalCount.Should().Be(2);
        response.Items.Should().AllSatisfy(p => p.Name.Should().StartWith("A-"));
    }

    [Fact]
    public async Task TenantB_OnlySees_OwnProjects()
    {
        var token = await LoginAs("userb@test.dev");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetFromJsonAsync<PagedResult<ProjectListItemDto>>("/api/projects");

        response.Should().NotBeNull();
        response!.TotalCount.Should().Be(1);
        response.Items.Should().AllSatisfy(p => p.Name.Should().StartWith("B-"));
    }

    [Fact]
    public async Task TenantA_CannotSee_TenantB_Projects()
    {
        var token = await LoginAs("usera@test.dev");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetFromJsonAsync<PagedResult<ProjectListItemDto>>("/api/projects?search=B-Project");

        response.Should().NotBeNull();
        response!.TotalCount.Should().Be(0);
    }

    private async Task<string> LoginAs(string email)
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "TestPass1"));
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }
}
