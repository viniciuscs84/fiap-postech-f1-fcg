using FCG.Application.Authentication;
using FCG.Infrastructure.Persistence;
using FCG.Migrations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FCG.Tests;

public sealed class TestAppFactory : WebApplicationFactory<global::Program>, IAsyncDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"fcg-tests-{Guid.NewGuid():N}.db");
    private readonly string connectionString;

    public TestAppFactory()
    {
        connectionString = $"Data Source={databasePath}";
        MigrationRunner.ApplyAsync(connectionString).GetAwaiter().GetResult();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddDebug();
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{JwtOptions.SectionName}:Issuer"] = "FCG",
                [$"{JwtOptions.SectionName}:Audience"] = "FCG",
                [$"{JwtOptions.SectionName}:SigningKey"] = "TEST-ONLY-KEY-CHANGE-ME-1234567890",
                [$"{JwtOptions.SectionName}:ExpirationMinutes"] = "60",
                ["ConnectionStrings:DefaultConnection"] = connectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
