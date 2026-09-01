namespace FCG.Application.Games;

public abstract record GameRegistrationOutcome
{
    private GameRegistrationOutcome()
    {
    }

    public sealed record Success(RegisteredGameResponse Game) : GameRegistrationOutcome;

    public sealed record ValidationFailure(IReadOnlyDictionary<string, string[]> Errors) : GameRegistrationOutcome;
}

public sealed record RegisteredGameResponse(
    Guid Id,
    string Title,
    string Description,
    string Genre,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc);
