using FCG.Domain.Promotions;

namespace FCG.Application.Promotions;

public interface IPromotionRepository
{
    Task<Promotion?> FindByNormalizedCodeAsync(string normalizedCode, CancellationToken cancellationToken);

    Task AddAsync(Promotion promotion, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
