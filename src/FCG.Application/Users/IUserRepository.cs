using FCG.Domain.Users;

namespace FCG.Application.Users;

/// <summary>Persistence boundary for user accounts.</summary>
public interface IUserRepository
{
    /// <summary>Finds a user by its normalized e-mail.</summary>
    Task<UserAccount?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Adds a user to the current persistence unit of work.</summary>
    Task AddAsync(UserAccount user, CancellationToken cancellationToken);

    /// <summary>Persists pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
