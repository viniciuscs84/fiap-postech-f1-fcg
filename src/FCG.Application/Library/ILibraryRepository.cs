using FCG.Application.Library;

namespace FCG.Application.Library;

/// <summary>Persistence boundary for acquired games.</summary>
public interface ILibraryRepository
{
    /// <summary>Finds all acquired games for a user.</summary>
    Task<IReadOnlyList<LibraryItemResponse>> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
