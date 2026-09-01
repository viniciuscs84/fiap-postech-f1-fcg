namespace FCG.Application.Library;

public sealed class LibraryService(ILibraryRepository repository) : ILibraryService
{
    public Task<IReadOnlyList<LibraryItemResponse>> GetMyLibraryAsync(Guid userId, CancellationToken cancellationToken)
    {
        return repository.FindByUserIdAsync(userId, cancellationToken);
    }
}
