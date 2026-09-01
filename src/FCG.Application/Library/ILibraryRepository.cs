using FCG.Application.Library;

namespace FCG.Application.Library;

public interface ILibraryRepository
{
    Task<IReadOnlyList<LibraryItemResponse>> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
