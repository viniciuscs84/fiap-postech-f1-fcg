using FCG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FCG.Migrations;

/// <summary>Creates the application context for EF Core design-time commands.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>Creates an SQLite-configured application context.</summary>
    /// <param name="args">Design-time arguments.</param>
    /// <returns>The configured application context.</returns>
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=fcg.db", sqlite =>
                sqlite.MigrationsAssembly(typeof(MigrationAssemblyMarker).Assembly.GetName().Name))
            .Options;

        return new AppDbContext(options);
    }
}
