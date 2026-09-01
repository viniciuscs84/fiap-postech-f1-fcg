using FCG.Application.Library;
using FCG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCG.Infrastructure.Library;

/// <summary>EF Core repository for acquired game libraries.</summary>
public sealed class EfLibraryRepository(AppDbContext dbContext) : ILibraryRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LibraryItemResponse>> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.AcquiredGames
            .AsNoTracking()
            .Where(acquisition => acquisition.UserId == userId)
            .OrderBy(acquisition => acquisition.AcquiredAtUtc)
            .Select(acquisition => new LibraryItemResponse(
                acquisition.GameId,
                acquisition.Game.Title,
                acquisition.Game.Description,
                acquisition.Game.Genre,
                acquisition.AcquiredAtUtc))
            .ToListAsync(cancellationToken);
    }
}
