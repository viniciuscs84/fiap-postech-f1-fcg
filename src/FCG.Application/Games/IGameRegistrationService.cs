namespace FCG.Application.Games;

public interface IGameRegistrationService
{
    Task<GameRegistrationOutcome> RegisterAsync(CreateGameCommand command, Guid createdByUserId, CancellationToken cancellationToken);
}
