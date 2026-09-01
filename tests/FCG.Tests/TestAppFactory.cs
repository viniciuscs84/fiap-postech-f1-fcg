using FCG.Application.Authentication;
using FCG.Infrastructure.Persistence;
using FCG.Migrations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FCG.Tests;

public sealed class TestAppFactory : WebApplicationFactory<global::Program>, IAsyncDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public TestAppFactory()
    {
        connection.Open();
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
                [$"{BootstrapAdminOptions.SectionName}:Enabled"] = "true",
                [$"{BootstrapAdminOptions.SectionName}:Name"] = "Administrator",
                [$"{BootstrapAdminOptions.SectionName}:Email"] = "admin@example.com",
                [$"{BootstrapAdminOptions.SectionName}:Password"] = "Admin123!"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection, sqlite =>
                sqlite.MigrationsAssembly(typeof(MigrationAssemblyMarker).Assembly.GetName().Name)));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await connection.CloseAsync();
        await connection.DisposeAsync();
        await base.DisposeAsync();
    }
}
