using FCG.Domain.Games;

namespace FCG.Application.Games;

public sealed class GameRegistrationService(IGameRepository repository) : IGameRegistrationService
{
    public async Task<GameRegistrationOutcome> RegisterAsync(CreateGameCommand command, Guid createdByUserId, CancellationToken cancellationToken)
    {
        var validationErrors = GameRegistrationRules.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new GameRegistrationOutcome.ValidationFailure(validationErrors);
        }

        var game = Game.Create(
            GameRegistrationRules.Normalize(command.Title),
            GameRegistrationRules.Normalize(command.Description),
            GameRegistrationRules.Normalize(command.Genre),
            createdByUserId,
            DateTime.UtcNow);

        await repository.AddAsync(game, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new GameRegistrationOutcome.Success(new RegisteredGameResponse(
            game.Id,
            game.Title,
            game.Description,
            game.Genre,
            game.CreatedByUserId,
            game.CreatedAtUtc));
    }
}
