namespace FCG.Application.Promotions;

public interface IPromotionRegistrationService
{
    Task<PromotionRegistrationOutcome> RegisterAsync(CreatePromotionCommand command, Guid createdByUserId, CancellationToken cancellationToken);
}
