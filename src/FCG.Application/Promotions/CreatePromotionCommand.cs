namespace FCG.Application.Promotions;

public sealed record CreatePromotionCommand(
    string Name,
    string Code,
    decimal DiscountPercentage,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc);
