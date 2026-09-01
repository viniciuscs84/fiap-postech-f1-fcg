namespace FCG.Application.Library;

/// <summary>Provides library use cases.</summary>
public interface ILibraryService
{
    /// <summary>Returns the authenticated user's acquired games.</summary>
    Task<IReadOnlyList<LibraryItemResponse>> GetMyLibraryAsync(Guid userId, CancellationToken cancellationToken);
}
