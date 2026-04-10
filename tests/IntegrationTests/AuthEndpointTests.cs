using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Solodoc.Shared.Auth;

namespace Solodoc.IntegrationTests;

public class AuthEndpointTests : IAsyncLifetime
{
    private readonly SolodocApiFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointTests()
    {
        _factory = new SolodocApiFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Register_ValidData_ReturnsSuccess()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Test User", "newuser@test.dev", "ValidPass1", "ValidPass1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
        auth.Person.Email.Should().Be("newuser@test.dev");
        auth.Person.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        // First registration
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("User One", "duplicate@test.dev", "ValidPass1", "ValidPass1"));

        // Duplicate
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("User Two", "duplicate@test.dev", "ValidPass1", "ValidPass1"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        error!.Error.Should().Contain("finnes allerede");
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Login User", "login@test.dev", "ValidPass1", "ValidPass1"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login@test.dev", "ValidPass1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
        auth.Person.Email.Should().Be("login@test.dev");
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsBadRequest()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Wrong Pass", "wrongpass@test.dev", "ValidPass1", "ValidPass1"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("wrongpass@test.dev", "WrongPassword1"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Refresh User", "refresh@test.dev", "ValidPass1", "ValidPass1"));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(auth!.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuth!.AccessToken.Should().NotBeNullOrEmpty();
        newAuth.RefreshToken.Should().NotBe(auth.RefreshToken, "should rotate the refresh token");
    }

    [Fact]
    public async Task Refresh_UsedToken_ReturnsUnauthorized()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Reuse User", "reuse@test.dev", "ValidPass1", "ValidPass1"));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // First refresh — should succeed
        await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(auth!.RefreshToken));

        // Second refresh with same token — should fail (token was revoked)
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(auth.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record ErrorDto(string? Error);
}
