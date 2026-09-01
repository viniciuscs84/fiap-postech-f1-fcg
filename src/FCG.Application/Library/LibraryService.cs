namespace FCG.Application.Library;

/// <summary>Implements library queries.</summary>
public sealed class LibraryService(ILibraryRepository repository) : ILibraryService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<LibraryItemResponse>> GetMyLibraryAsync(Guid userId, CancellationToken cancellationToken)
    {
        return repository.FindByUserIdAsync(userId, cancellationToken);
    }
}
