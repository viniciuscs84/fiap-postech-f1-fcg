using FCG.Domain.Users;
using FCG.Infrastructure.Persistence;
using FCG.Infrastructure.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FCG.Tests.Infrastructure;

public sealed class UserRepositoryPersistenceTests
{
    [Fact]
    public async Task Repository_persists_password_hash_and_rejects_duplicate_email()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var repository = new EfUserRepository(context);
        var firstUser = UserAccount.Register("Alice", "alice@example.com", "hash-one", UserRole.User, DateTime.UtcNow);

        await repository.AddAsync(firstUser, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var storedUser = await context.Users.SingleAsync();
        Assert.Equal("hash-one", storedUser.PasswordHash);
        Assert.Equal("ALICE@EXAMPLE.COM", storedUser.NormalizedEmail);

        var duplicate = UserAccount.Register("Alice Two", "alice@example.com", "hash-two", UserRole.User, DateTime.UtcNow);
        await repository.AddAsync(duplicate, CancellationToken.None);

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => repository.SaveChangesAsync(CancellationToken.None));
    }
}
