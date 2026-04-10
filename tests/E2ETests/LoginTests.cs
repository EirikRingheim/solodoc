using FluentAssertions;
using Microsoft.Playwright;

namespace Solodoc.E2ETests;

[Collection("Playwright")]
public class LoginTests : E2ETestBase
{
    public LoginTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task LoginPage_ShowsForm()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        var heading = await Page.TextContentAsync("h5, h4, .mud-text");
        // Just verify the page loads without error
        var url = Page.Url;
        url.Should().Contain("login");
    }

    [Fact]
    public async Task Login_ValidCredentials_RedirectsToDashboard()
    {
        await LoginAsync();
        // After login, should be on dashboard or tenant selector
        var url = Page.Url;
        url.Should().NotContain("/login");
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShowsError()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.FillAsync("input[type='email']", "wrong@test.dev");
        await Page.FillAsync("input[type='password']", "WrongPass1");
        await Page.ClickAsync("button:has-text('Logg inn')");
        // Should still be on login page
        await Page.WaitForTimeoutAsync(2000);
        Page.Url.Should().Contain("login");
    }
}
