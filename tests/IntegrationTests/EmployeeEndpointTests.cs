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
using Solodoc.Shared.Employees;

namespace Solodoc.IntegrationTests;

public class EmployeeEndpointTests : IAsyncLifetime
{
    private readonly SolodocApiFactory _factory;
    private readonly HttpClient _client;
    private Guid _tenantId;
    private Guid _adminPersonId;
    private Guid _workerPersonId;

    public EmployeeEndpointTests()
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

        var worker = new Person
        {
            Email = "worker@test.dev",
            FullName = "Field Worker",
            PasswordHash = hasher.Hash("TestPass1"),
            State = PersonState.Active,
            EmailVerified = true,
            PhoneNumber = "99887766"
        };
        db.Persons.Add(worker);
        _workerPersonId = worker.Id;

        db.TenantMemberships.AddRange(
            new TenantMembership
            {
                PersonId = admin.Id,
                TenantId = tenant.Id,
                Role = TenantRole.TenantAdmin,
                State = TenantMembershipState.Active
            },
            new TenantMembership
            {
                PersonId = worker.Id,
                TenantId = tenant.Id,
                Role = TenantRole.FieldWorker,
                State = TenantMembershipState.Active
            }
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

    private async Task AuthenticateAs(string email)
    {
        var token = await LoginAs(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task ListEmployees_AsAdmin_ReturnsEmployees()
    {
        await AuthenticateAs("admin@test.dev");

        var response = await _client.GetAsync("/api/employees");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var employees = await response.Content.ReadFromJsonAsync<List<EmployeeListItemDto>>();
        employees.Should().NotBeNull();
        employees!.Count.Should().BeGreaterThanOrEqualTo(2);
        employees.Should().Contain(e => e.Email == "admin@test.dev");
        employees.Should().Contain(e => e.Email == "worker@test.dev");
    }

    [Fact]
    public async Task GetEmployeeDetail_Exists_ReturnsDetail()
    {
        await AuthenticateAs("admin@test.dev");

        var response = await _client.GetAsync($"/api/employees/{_workerPersonId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<EmployeeDetailDto>();
        detail.Should().NotBeNull();
        detail!.FullName.Should().Be("Field Worker");
        detail.Email.Should().Be("worker@test.dev");
        detail.Role.Should().Be("Feltarbeider");
    }

    [Fact]
    public async Task InviteEmployee_ValidEmail_CreatesInvitation()
    {
        await AuthenticateAs("admin@test.dev");

        var response = await _client.PostAsJsonAsync("/api/employees/invite",
            new InviteEmployeeRequest("newguy@test.dev", "FieldWorker"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProfile_Authenticated_ReturnsProfile()
    {
        await AuthenticateAs("worker@test.dev");

        var response = await _client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile.Should().NotBeNull();
        profile!.FullName.Should().Be("Field Worker");
        profile.Email.Should().Be("worker@test.dev");
    }

    [Fact]
    public async Task UpdateProfile_ValidData_UpdatesName()
    {
        await AuthenticateAs("worker@test.dev");

        var response = await _client.PutAsJsonAsync("/api/profile",
            new UpdateProfileRequest("Updated Worker Name", "11223344", "Europe/Oslo"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile.Should().NotBeNull();
        profile!.FullName.Should().Be("Updated Worker Name");
        profile.Phone.Should().Be("11223344");
    }

    private record IdResponse(Guid Id);
}
