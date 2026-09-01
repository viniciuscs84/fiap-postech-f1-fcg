using FCG.Application.Promotions;
using FCG.Domain.Promotions;
using FCG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCG.Infrastructure.Promotions;

public sealed class EfPromotionRepository(AppDbContext dbContext) : IPromotionRepository
{
    public Task<Promotion?> FindByNormalizedCodeAsync(string normalizedCode, CancellationToken cancellationToken)
    {
        return dbContext.Promotions.SingleOrDefaultAsync(promotion => promotion.NormalizedCode == normalizedCode, cancellationToken);
    }

    public Task AddAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        return dbContext.Promotions.AddAsync(promotion, cancellationToken).AsTask();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
