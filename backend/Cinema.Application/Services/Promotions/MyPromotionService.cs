using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Promotions;

public class MyPromotionService(IAppDbContext db) : IMyPromotionService
{
    public async Task<List<MyPromotionDto>> GetPersonalCodesAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;

        var promotions = await db
            .Promotions.AsNoTracking()
            .Where(p =>
                p.Type == PromotionType.PersonalCode
                && p.AssignedUserId == userId
                && p.IsActive
                && (p.StartsAt == null || p.StartsAt <= now)
                && (p.EndsAt == null || p.EndsAt >= now)
                && (p.TotalUsageLimit == null || p.ConsumedCount < p.TotalUsageLimit)
                && (p.BudgetAmount == null || p.ConsumedBudget < p.BudgetAmount)
                && !db
                    .PromotionCustomerUsages.IgnoreQueryFilters()
                    .Any(u =>
                        u.PromotionId == p.PromotionId
                        && u.UserId == userId
                        && p.PerCustomerLimit != null
                        && u.ConsumedCount >= p.PerCustomerLimit
                    )
            )
            .OrderBy(p => p.EndsAt ?? DateTime.MaxValue)
            .ThenBy(p => p.PromotionId)
            .ToListAsync(cancellationToken);

        return promotions
            .Select(p => new MyPromotionDto(
                p.PromotionId,
                p.Code,
                p.Name,
                p.Description,
                p.DiscountType.ToString(),
                p.DiscountValue,
                p.MaxDiscountAmount,
                p.Scope.ToString(),
                p.MinOrderAmount,
                p.StartsAt,
                p.EndsAt,
                p.Channel?.ToString()
            ))
            .ToList();
    }
}
