using FCG.Domain.Games;

namespace FCG.Application.Games;

/// <summary>Persistence boundary for catalog games.</summary>
public interface IGameRepository
{
    /// <summary>Adds a game to the current persistence unit of work.</summary>
    Task AddAsync(Game game, CancellationToken cancellationToken);

    /// <summary>Persists pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
