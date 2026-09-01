namespace FCG.Domain.Games;

public sealed class Game
{
    private Game()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Genre { get; private set; } = string.Empty;

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Game Create(string title, string description, string genre, Guid createdByUserId, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(genre);

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("A creator user id is required.", nameof(createdByUserId));
        }

        return new Game
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            Genre = genre.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc)
        };
    }
}
