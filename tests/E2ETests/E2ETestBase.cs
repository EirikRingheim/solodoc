using Microsoft.Playwright;

namespace Solodoc.E2ETests;

[Collection("Playwright")]
public abstract class E2ETestBase : IAsyncLifetime
{
    protected readonly PlaywrightFixture Fixture;
    protected IPage Page = null!;
    protected IBrowserContext Context = null!;

    // Default: mobile viewport
    protected virtual int ViewportWidth => 375;
    protected virtual int ViewportHeight => 812;

    protected string BaseUrl => Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5200";

    protected E2ETestBase(PlaywrightFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        Context = await Fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = ViewportWidth, Height = ViewportHeight }
        });
        Page = await Context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    protected async Task LoginAsync(string email = "admin@solodoc.dev", string password = "Admin1234!")
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.FillAsync("input[type='email']", email);
        await Page.FillAsync("input[type='password']", password);
        await Page.ClickAsync("button:has-text('Logg inn')");
        // Wait for redirect to dashboard or tenant selector
        await Page.WaitForURLAsync(url => !url.Contains("/login"), new() { Timeout = 10000 });
    }
}

[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>;
