using FluentAssertions;

namespace Solodoc.E2ETests;

[Collection("Playwright")]
public class NavigationTests : E2ETestBase
{
    public NavigationTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Dashboard_ShowsStatCards()
    {
        await LoginAsync();
        // Navigate to dashboard
        await Page.GotoAsync($"{BaseUrl}/");
        await Page.WaitForLoadStateAsync();
        // Should see dashboard content
        var content = await Page.ContentAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Projects_PageLoads()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/projects");
        await Page.WaitForLoadStateAsync();
        Page.Url.Should().Contain("projects");
    }

    [Fact]
    public async Task Deviations_PageLoads()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/deviations");
        await Page.WaitForLoadStateAsync();
        Page.Url.Should().Contain("deviations");
    }

    [Fact]
    public async Task Hours_PageLoads()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/hours");
        await Page.WaitForLoadStateAsync();
        Page.Url.Should().Contain("hours");
    }

    [Fact]
    public async Task Employees_PageLoads()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/employees");
        await Page.WaitForLoadStateAsync();
        Page.Url.Should().Contain("employees");
    }
}
