using FCG.Application.Games;
using FCG.Domain.Games;
using FCG.Infrastructure.Persistence;

namespace FCG.Infrastructure.Games;

public sealed class EfGameRepository(AppDbContext dbContext) : IGameRepository
{
    public Task AddAsync(Game game, CancellationToken cancellationToken)
    {
        return dbContext.Games.AddAsync(game, cancellationToken).AsTask();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
