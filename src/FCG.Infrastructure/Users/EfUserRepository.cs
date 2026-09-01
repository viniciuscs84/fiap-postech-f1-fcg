using FCG.Application.Users;
using FCG.Domain.Users;
using FCG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCG.Infrastructure.Users;

/// <summary>EF Core repository for user accounts.</summary>
public sealed class EfUserRepository(AppDbContext dbContext) : IUserRepository
{
    /// <inheritdoc />
    public Task<UserAccount?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.Users.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(UserAccount user, CancellationToken cancellationToken)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
