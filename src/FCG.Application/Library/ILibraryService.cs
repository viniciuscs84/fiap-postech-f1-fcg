namespace FCG.Application.Library;

public interface ILibraryService
{
    Task<IReadOnlyList<LibraryItemResponse>> GetMyLibraryAsync(Guid userId, CancellationToken cancellationToken);
}
