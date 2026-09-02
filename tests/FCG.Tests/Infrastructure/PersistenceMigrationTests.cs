using FCG.Domain.Games;
using FCG.Domain.Library;
using FCG.Domain.Promotions;
using FCG.Domain.Users;
using FCG.Infrastructure.Persistence;
using FCG.Migrations;
using Microsoft.EntityFrameworkCore;

namespace FCG.Tests.Infrastructure;

public sealed class PersistenceMigrationTests
{
    [Fact]
    public async Task Migration_runner_creates_schema_seeds_admin_and_data_survives_restart()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";

        await MigrationRunner.ApplyAsync(connectionString);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        await using (var context = new AppDbContext(options))
        {
            var seededAdmin = await context.Users.SingleAsync(user => user.Email == "admin@example.com");
            Assert.Equal(UserRole.Administrator, seededAdmin.Role);

            var user = UserAccount.Register("Alice", "alice@example.com", "hash", UserRole.User, DateTime.UtcNow);
            var game = Game.Create("Halo", "Sci-fi shooter", "Action", user.Id, DateTime.UtcNow);
            var promotion = Promotion.Create("Launch Weekend", "LAUNCH25", 25, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), user.Id, DateTime.UtcNow);

            context.Users.Add(user);
            context.Games.Add(game);
            context.Promotions.Add(promotion);
            context.AcquiredGames.Add(AcquiredGame.Acquire(user.Id, game.Id, DateTime.UtcNow));

            await context.SaveChangesAsync();
        }

        await using (var reopenedContext = new AppDbContext(options))
        {
            Assert.Equal(2, await reopenedContext.Users.CountAsync());
            Assert.Equal(1, await reopenedContext.Games.CountAsync());
            Assert.Equal(1, await reopenedContext.Promotions.CountAsync());
            Assert.Equal(1, await reopenedContext.AcquiredGames.CountAsync());

            var storedUser = await reopenedContext.Users.SingleAsync(user => user.Email == "alice@example.com");
            var storedGame = await reopenedContext.Games.SingleAsync();
            var storedPromotion = await reopenedContext.Promotions.SingleAsync();
            var storedOwnership = await reopenedContext.AcquiredGames.SingleAsync();

            Assert.Equal("Alice", storedUser.Name);
            Assert.Equal("Halo", storedGame.Title);
            Assert.Equal("LAUNCH25", storedPromotion.Code);
            Assert.Equal(storedUser.Id, storedOwnership.UserId);
            Assert.Equal(storedGame.Id, storedOwnership.GameId);
        }

        File.Delete(databasePath);
    }

    [Fact]
    public async Task Duplicate_unique_relations_are_rejected_after_migrations_are_applied()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";

        await MigrationRunner.ApplyAsync(connectionString);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        await using var context = new AppDbContext(options);

        var user = UserAccount.Register("Alice", "alice@example.com", "hash", UserRole.User, DateTime.UtcNow);
        var game = Game.Create("Halo", "Sci-fi shooter", "Action", user.Id, DateTime.UtcNow);
        var promotion = Promotion.Create("Launch Weekend", "LAUNCH25", 25, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), user.Id, DateTime.UtcNow);

        context.Users.Add(user);
        context.Games.Add(game);
        context.Promotions.Add(promotion);
        context.AcquiredGames.Add(AcquiredGame.Acquire(user.Id, game.Id, DateTime.UtcNow));
        await context.SaveChangesAsync();

        context.Promotions.Add(Promotion.Create("Another Promotion", "LAUNCH25", 15, DateTime.UtcNow, DateTime.UtcNow.AddDays(3), user.Id, DateTime.UtcNow));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());

        File.Delete(databasePath);
    }
}
