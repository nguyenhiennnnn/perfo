using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Promotions;

public class PromotionResourceHold(IAppDbContext db) : IResourceHold
{
    public string ResourceType => nameof(Promotion);

    public async Task ReserveAsync(ResourceHoldRequest request, CancellationToken cancellationToken)
    {
        var order = request.Order;
        var adjustments = order
            .PriceAdjustments.Where(a =>
                a.Type == PriceAdjustmentType.Promotion && a.ReferenceId != null && a.Amount > 0m
            )
            .OrderBy(a => a.ReferenceId)
            .ToList();

        var now = DateTime.UtcNow;
        foreach (var adjustment in adjustments)
        {
            var promotionId = adjustment.ReferenceId!.Value;
            await ConsumeAsync(promotionId, order.UserId, adjustment.Amount, cancellationToken);
            db.PromotionRedemptions.Add(
                new PromotionRedemption
                {
                    PromotionId = promotionId,
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    Status = PromotionRedemptionStatus.Reserved,
                    DiscountAmount = adjustment.Amount,
                    ReservedAt = now,
                }
            );
        }
    }

    public Task CommitAsync(int orderId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return db
            .PromotionRedemptions.IgnoreQueryFilters()
            .Where(r => r.OrderId == orderId && r.Status == PromotionRedemptionStatus.Reserved)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(r => r.Status, PromotionRedemptionStatus.Redeemed)
                        .SetProperty(r => r.RedeemedAt, (DateTime?)now),
                cancellationToken
            );
    }

    public async Task ReleaseAsync(int orderId, CancellationToken cancellationToken)
    {
        var reserved = await db
            .PromotionRedemptions.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.OrderId == orderId && r.Status == PromotionRedemptionStatus.Reserved)
            .OrderBy(r => r.PromotionId)
            .Select(r => new
            {
                r.PromotionRedemptionId,
                r.PromotionId,
                r.UserId,
                r.DiscountAmount,
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var redemption in reserved)
        {
            var claimed = await db
                .PromotionRedemptions.IgnoreQueryFilters()
                .Where(r =>
                    r.PromotionRedemptionId == redemption.PromotionRedemptionId
                    && r.Status == PromotionRedemptionStatus.Reserved
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(r => r.Status, PromotionRedemptionStatus.Released)
                            .SetProperty(r => r.ReleasedAt, (DateTime?)now),
                    cancellationToken
                );
            if (claimed == 0)
                continue;

            var amount = redemption.DiscountAmount;
            await db
                .Promotions.IgnoreQueryFilters()
                .Where(p => p.PromotionId == redemption.PromotionId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(p => p.ConsumedCount, p => p.ConsumedCount - 1)
                            .SetProperty(p => p.ConsumedBudget, p => p.ConsumedBudget - amount),
                    cancellationToken
                );

            if (redemption.UserId is not { } userId)
                continue;

            await db
                .PromotionCustomerUsages.IgnoreQueryFilters()
                .Where(u => u.PromotionId == redemption.PromotionId && u.UserId == userId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(u => u.ConsumedCount, u => u.ConsumedCount - 1),
                    cancellationToken
                );
        }
    }

    private async Task ConsumeAsync(
        int promotionId,
        int? userId,
        decimal amount,
        CancellationToken cancellationToken
    )
    {
        var consumed = await db
            .Promotions.Where(p =>
                p.PromotionId == promotionId
                && p.IsActive
                && (p.TotalUsageLimit == null || p.ConsumedCount < p.TotalUsageLimit)
                && (p.BudgetAmount == null || p.ConsumedBudget + amount <= p.BudgetAmount)
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(p => p.ConsumedCount, p => p.ConsumedCount + 1)
                        .SetProperty(p => p.ConsumedBudget, p => p.ConsumedBudget + amount),
                cancellationToken
            );
        if (consumed == 0)
            throw await RejectAsync(promotionId, cancellationToken);

        if (userId is not { } customerId)
            return;

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"PromotionCustomerUsages\" (\"PromotionId\", \"UserId\", \"ConsumedCount\") VALUES ({promotionId}, {customerId}, 0) ON CONFLICT (\"PromotionId\", \"UserId\") DO NOTHING",
            cancellationToken
        );

        var perCustomerLimit = await db
            .Promotions.AsNoTracking()
            .Where(p => p.PromotionId == promotionId)
            .Select(p => p.PerCustomerLimit)
            .FirstAsync(cancellationToken);

        var customerConsumed = await db
            .PromotionCustomerUsages.IgnoreQueryFilters()
            .Where(u =>
                u.PromotionId == promotionId
                && u.UserId == customerId
                && (perCustomerLimit == null || u.ConsumedCount < perCustomerLimit)
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(u => u.ConsumedCount, u => u.ConsumedCount + 1),
                cancellationToken
            );
        if (customerConsumed == 0)
            throw PromotionRejections.ToException(
                PromotionRejections.Invalid(PromotionRejectionReason.CustomerLimitReached)
            );
    }

    private async Task<PromotionRejectedException> RejectAsync(
        int promotionId,
        CancellationToken cancellationToken
    )
    {
        var state = await db
            .Promotions.AsNoTracking()
            .Where(p => p.PromotionId == promotionId)
            .Select(p => new
            {
                p.IsActive,
                p.TotalUsageLimit,
                p.ConsumedCount,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var reason = state switch
        {
            null or { IsActive: false } => PromotionRejectionReason.NotFound,
            { TotalUsageLimit: { } limit, ConsumedCount: var count } when count >= limit =>
                PromotionRejectionReason.UsageLimitReached,
            _ => PromotionRejectionReason.BudgetExhausted,
        };
        return PromotionRejections.ToException(PromotionRejections.Invalid(reason));
    }
}
