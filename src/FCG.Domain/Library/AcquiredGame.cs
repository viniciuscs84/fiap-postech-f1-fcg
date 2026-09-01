namespace FCG.Domain.Library;

public sealed class AcquiredGame
{
    private AcquiredGame()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid GameId { get; private set; }

    public DateTime AcquiredAtUtc { get; private set; }

    public Users.UserAccount User { get; private set; } = null!;

    public Games.Game Game { get; private set; } = null!;

    public static AcquiredGame Acquire(Guid userId, Guid gameId, DateTime acquiredAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user id is required.", nameof(userId));
        }

        if (gameId == Guid.Empty)
        {
            throw new ArgumentException("A game id is required.", nameof(gameId));
        }

        return new AcquiredGame
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            AcquiredAtUtc = DateTime.SpecifyKind(acquiredAtUtc, DateTimeKind.Utc)
        };
    }
}
