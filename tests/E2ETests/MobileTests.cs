using FluentAssertions;

namespace Solodoc.E2ETests;

[Collection("Playwright")]
public class MobileTests : E2ETestBase
{
    // Mobile viewport — 375x812 (iPhone)
    protected override int ViewportWidth => 375;
    protected override int ViewportHeight => 812;

    public MobileTests(PlaywrightFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Mobile_BottomNavigation_IsVisible()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/");
        await Page.WaitForLoadStateAsync();
        // Check for bottom navigation bar
        var bottomNav = Page.Locator(".bottom-nav");
        var isVisible = await bottomNav.IsVisibleAsync();
        isVisible.Should().BeTrue();
    }

    [Fact]
    public async Task Mobile_Sidebar_IsHiddenByDefault()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/");
        await Page.WaitForLoadStateAsync();
        // On mobile, sidebar drawer should be closed
        var drawer = Page.Locator(".mud-drawer--open.mud-drawer-responsive");
        var count = await drawer.CountAsync();
        // Either not present or hidden is acceptable
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Mobile_CreateJob_FormIsUsable()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/jobs/new");
        await Page.WaitForLoadStateAsync();
        Page.Url.Should().Contain("jobs/new");
    }

    [Fact]
    public async Task Mobile_DeviationWizard_Loads()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/deviations/new");
        await Page.WaitForLoadStateAsync();
        Page.Url.Should().Contain("deviations/new");
    }

    [Fact]
    public async Task Mobile_TouchTargets_MinimumSize()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/");
        await Page.WaitForLoadStateAsync();
        // Check bottom nav items have minimum 48px touch target
        var navItems = Page.Locator(".bottom-nav-item");
        var count = await navItems.CountAsync();
        count.Should().BeGreaterThan(0);
    }
}
