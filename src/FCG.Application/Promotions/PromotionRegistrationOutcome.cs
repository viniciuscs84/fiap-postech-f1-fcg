namespace FCG.Application.Promotions;

public abstract record PromotionRegistrationOutcome
{
    private PromotionRegistrationOutcome()
    {
    }

    public sealed record Success(RegisteredPromotionResponse Promotion) : PromotionRegistrationOutcome;

    public sealed record ValidationFailure(IReadOnlyDictionary<string, string[]> Errors) : PromotionRegistrationOutcome;

    public sealed record Conflict : PromotionRegistrationOutcome;
}

public sealed record RegisteredPromotionResponse(
    Guid Id,
    string Name,
    string Code,
    decimal DiscountPercentage,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc);
