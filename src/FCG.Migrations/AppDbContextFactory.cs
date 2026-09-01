using FCG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FCG.Migrations;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=fcg.db", sqlite =>
                sqlite.MigrationsAssembly(typeof(MigrationAssemblyMarker).Assembly.GetName().Name))
            .Options;

        return new AppDbContext(options);
    }
}
