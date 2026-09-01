namespace FCG.Application.Library;

public sealed record LibraryItemResponse(
    Guid GameId,
    string Title,
    string Description,
    string Genre,
    DateTime AcquiredAtUtc);
