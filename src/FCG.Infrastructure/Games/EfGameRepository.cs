using FCG.Application.Games;
using FCG.Domain.Games;
using FCG.Infrastructure.Persistence;

namespace FCG.Infrastructure.Games;

/// <summary>EF Core repository for catalog games.</summary>
public sealed class EfGameRepository(AppDbContext dbContext) : IGameRepository
{
    /// <inheritdoc />
    public Task AddAsync(Game game, CancellationToken cancellationToken)
    {
        return dbContext.Games.AddAsync(game, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
