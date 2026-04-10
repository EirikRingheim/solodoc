using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Solodoc.Infrastructure.Persistence;

namespace Solodoc.IntegrationTests;

public class SolodocApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"solodoc_test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "TestSecretKeyThatIsAtLeast32CharactersLongForHmacSha256!",
                ["Jwt:Issuer"] = "solodoc.test",
                ["Jwt:Audience"] = "solodoc.test",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                ["ConnectionStrings:DefaultConnection"] =
                    $"Host=localhost;Port=5432;Database={_dbName};Username=solodoc;Password=solodoc_dev_password"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SolodocDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<SolodocDbContext>(options =>
                options.UseNpgsql(
                    $"Host=localhost;Port=5432;Database={_dbName};Username=solodoc;Password=solodoc_dev_password")
                    .UseSnakeCaseNamingConvention());

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SolodocDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SolodocDbContext>();
            await db.Database.EnsureDeletedAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }
        await base.DisposeAsync();
    }
}
