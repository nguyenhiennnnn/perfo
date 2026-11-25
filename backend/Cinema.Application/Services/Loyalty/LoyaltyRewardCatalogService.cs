using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Loyalty;

public class LoyaltyRewardCatalogService(IAppDbContext db) : ILoyaltyRewardCatalogService
{
    public async Task<IReadOnlyList<LoyaltyRewardCatalogItemDto>> GetRedeemableAsync(
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var items = await db
            .LoyaltyRewards.AsNoTracking()
            .Where(r =>
                r.IsActive
                && r.RemainingQuantity > 0
                && (r.ValidFrom == null || r.ValidFrom <= now)
                && (r.ValidTo == null || r.ValidTo > now)
                && (r.ComboId == null || r.Combo!.IsActive)
            )
            .OrderBy(r => r.PointsCost)
            .ThenBy(r => r.RewardId)
            .Select(r => new { Reward = r, ComboName = r.Combo != null ? r.Combo.Name : null })
            .ToListAsync(cancellationToken);

        return items.Select(i => LoyaltyRewardMapper.ToCatalogItem(i.Reward, i.ComboName)).ToList();
    }
}
