using FCG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCG.Migrations;

/// <summary>Executes all pending database migrations for the configured database.</summary>
public static class MigrationRunner
{
    /// <summary>Applies pending migrations to the target database.</summary>
    /// <param name="connectionString">SQLite connection string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ApplyAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A database connection string is required.", nameof(connectionString));
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString, sqlite =>
                sqlite.MigrationsAssembly(typeof(MigrationAssemblyMarker).Assembly.GetName().Name))
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}

internal static class Program
{
    private const string DefaultConnectionString = "Data Source=fcg.db";

    private static async Task<int> Main(string[] args)
    {
        try
        {
            var connectionString = ResolveConnectionString(args);
            await MigrationRunner.ApplyAsync(connectionString);
            Console.WriteLine("Database migrations applied successfully.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Database migration failed: {exception.Message}");
            return 1;
        }
    }

    private static string ResolveConnectionString(string[] args)
    {
        const string argumentPrefix = "--connection=";
        var argument = args.FirstOrDefault(value => value.StartsWith(argumentPrefix, StringComparison.OrdinalIgnoreCase));
        if (argument is not null)
        {
            return argument[argumentPrefix.Length..];
        }

        return Environment.GetEnvironmentVariable("FCG_CONNECTION_STRING") ?? DefaultConnectionString;
    }
}
