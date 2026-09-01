using FCG.Domain.Users;

namespace FCG.Application.Users;

public interface IUserRepository
{
    Task<UserAccount?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task AddAsync(UserAccount user, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
