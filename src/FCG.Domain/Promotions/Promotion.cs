namespace FCG.Domain.Promotions;

public sealed class Promotion
{
    private Promotion()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string NormalizedCode { get; private set; } = string.Empty;

    public decimal DiscountPercentage { get; private set; }

    public DateTime StartsAtUtc { get; private set; }

    public DateTime EndsAtUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Promotion Create(
        string name,
        string code,
        decimal discountPercentage,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        Guid createdByUserId,
        DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (discountPercentage <= 0 || discountPercentage > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(discountPercentage), "The discount percentage must be between 0 and 100.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("A creator user id is required.", nameof(createdByUserId));
        }

        var normalizedStartsAtUtc = DateTime.SpecifyKind(startsAtUtc, DateTimeKind.Utc);
        var normalizedEndsAtUtc = DateTime.SpecifyKind(endsAtUtc, DateTimeKind.Utc);
        if (normalizedEndsAtUtc <= normalizedStartsAtUtc)
        {
            throw new ArgumentException("The promotion end date must be after the start date.");
        }

        var trimmedName = name.Trim();
        var trimmedCode = code.Trim();

        return new Promotion
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Code = trimmedCode,
            NormalizedCode = trimmedCode.ToUpperInvariant(),
            DiscountPercentage = discountPercentage,
            StartsAtUtc = normalizedStartsAtUtc,
            EndsAtUtc = normalizedEndsAtUtc,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc)
        };
    }
}
