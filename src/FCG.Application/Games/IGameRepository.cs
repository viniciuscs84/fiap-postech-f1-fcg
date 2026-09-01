using FCG.Domain.Games;

namespace FCG.Application.Games;

public interface IGameRepository
{
    Task AddAsync(Game game, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
