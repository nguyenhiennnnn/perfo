using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Abuse;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Promotions;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Promotions;

public class PromotionEngine(
    IAppDbContext db,
    IAppCache cache,
    ICustomerIdentityLinkService identityLinks
) : IPromotionEngine
{
    public async Task<PromotionCheckResult> ValidateAsync(
        string code,
        PromotionContext context,
        CancellationToken cancellationToken
    )
    {
        var resolution = await ResolveAsync(context, code, false, cancellationToken);
        return resolution.CodeResult
            ?? PromotionRejections.Invalid(PromotionRejectionReason.NotFound);
    }

    public async Task<PromotionResolution> ResolveAsync(
        PromotionContext context,
        string? code,
        bool includeAutoApply,
        CancellationToken cancellationToken
    )
    {
        var active = await cache.GetAsync(CacheKeys.Promotions, cancellationToken);
        var candidates = new List<PromotionSnapshot>();
        if (includeAutoApply)
            candidates.AddRange(active.Where(p => p.Type == PromotionType.AutoApply));

        PromotionSnapshot? requested = null;
        if (!string.IsNullOrWhiteSpace(code))
        {
            requested = await FindByCodeAsync(code, active, cancellationToken);
            if (requested is null)
                return new PromotionResolution(
                    [],
                    PromotionRejections.Invalid(PromotionRejectionReason.NotFound)
                );

            if (candidates.All(c => c.PromotionId != requested.PromotionId))
                candidates.Add(requested);
        }

        if (candidates.Count == 0)
            return PromotionResolution.Empty;

        var failures = new Dictionary<int, PromotionCheckResult>();
        var passed = new List<PromotionSnapshot>();
        foreach (var candidate in candidates)
        {
            if (PromotionConditions.CheckEligibility(candidate, context) is { } reason)
                failures[candidate.PromotionId] = PromotionRejections.Invalid(reason, candidate);
            else
                passed.Add(candidate);
        }

        await RejectReturningCustomersAsync(passed, context, failures, cancellationToken);

        var usage = await LoadUsageAsync(
            passed.Select(p => p.PromotionId).ToList(),
            context,
            cancellationToken
        );
        var eligible = new List<EligiblePromotion>();
        foreach (var promotion in passed)
        {
            var promotionUsage = usage.GetValueOrDefault(promotion.PromotionId, PromotionUsage.None);
            if (
                PromotionConditions.CheckUsage(promotion, promotionUsage, context.UserId is not null)
                is { } reason
            )
                failures[promotion.PromotionId] = PromotionRejections.Invalid(reason, promotion);
            else
                eligible.Add(new EligiblePromotion(promotion, promotionUsage));
        }

        var selection = PromotionSelector.Select(eligible, context.Lines);
        foreach (var (promotion, reason) in selection.Rejected)
            failures[promotion.PromotionId] = PromotionRejections.Invalid(reason, promotion);

        PromotionCheckResult? codeResult = null;
        if (requested is not null)
        {
            var applied = selection.Applied.FirstOrDefault(a =>
                a.Promotion.PromotionId == requested.PromotionId
            );
            codeResult = applied is not null
                ? PromotionCheckResult.Valid(applied.Amount)
                : failures.GetValueOrDefault(requested.PromotionId)
                    ?? PromotionRejections.Invalid(PromotionRejectionReason.NotCombinable, requested);
        }

        return new PromotionResolution(selection.Applied, codeResult);
    }

    private async Task<PromotionSnapshot?> FindByCodeAsync(
        string code,
        IReadOnlyList<PromotionSnapshot> active,
        CancellationToken cancellationToken
    )
    {
        var normalized = PromotionMapper.NormalizeCode(code);
        var cached = active.FirstOrDefault(p => p.Code == normalized);
        if (cached is not null)
            return cached;

        var entity = await db
            .Promotions.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Code == normalized && p.IsActive && !p.IsDeleted,
                cancellationToken
            );
        return entity is null ? null : PromotionSnapshot.From(entity);
    }

    private async Task RejectReturningCustomersAsync(
        List<PromotionSnapshot> passed,
        PromotionContext context,
        Dictionary<int, PromotionCheckResult> failures,
        CancellationToken cancellationToken
    )
    {
        if (context.UserId is not { } userId || !passed.Any(p => p.NewCustomersOnly))
            return;

        var hasPurchased = await db.Orders.AnyAsync(
            o =>
                o.UserId == userId
                && o.Status == OrderStatus.Confirmed
                && (o.Type == OrderType.Ticket || o.Type == OrderType.Combo),
            cancellationToken
        );
        var sharedIdentityUsed = !hasPurchased
            && await SharedIdentityUsedNewCustomerOfferAsync(userId, context, cancellationToken);
        if (!hasPurchased && !sharedIdentityUsed)
            return;

        var reason = hasPurchased
            ? PromotionRejectionReason.NewCustomersOnly
            : PromotionRejectionReason.NewCustomerSharedIdentity;
        foreach (var promotion in passed.Where(p => p.NewCustomersOnly).ToList())
        {
            failures[promotion.PromotionId] = PromotionRejections.Invalid(reason, promotion);
            passed.Remove(promotion);
        }
    }

    private async Task<bool> SharedIdentityUsedNewCustomerOfferAsync(
        int userId,
        PromotionContext context,
        CancellationToken cancellationToken
    )
    {
        var linkedIds = (await identityLinks.FindLinkedUserIdsAsync(userId, cancellationToken))
            .ToList();
        if (linkedIds.Count == 0)
            return false;

        var hasPurchased = await db.Orders.AnyAsync(
            o =>
                o.UserId != null
                && linkedIds.Contains(o.UserId.Value)
                && o.Status == OrderStatus.Confirmed
                && (o.Type == OrderType.Ticket || o.Type == OrderType.Combo),
            cancellationToken
        );
        if (hasPurchased)
            return true;

        return await db.PromotionRedemptions.AnyAsync(
            r =>
                r.UserId != null
                && linkedIds.Contains(r.UserId.Value)
                && r.Status != PromotionRedemptionStatus.Released
                && r.Promotion.NewCustomersOnly
                && (context.OrderId == null || r.OrderId != context.OrderId),
            cancellationToken
        );
    }

    private async Task<Dictionary<int, PromotionUsage>> LoadUsageAsync(
        List<int> promotionIds,
        PromotionContext context,
        CancellationToken cancellationToken
    )
    {
        var result = new Dictionary<int, PromotionUsage>();
        if (promotionIds.Count == 0)
            return result;

        var query = db.PromotionRedemptions.AsNoTracking()
            .Where(r =>
                promotionIds.Contains(r.PromotionId)
                && r.Status != PromotionRedemptionStatus.Released
            );
        if (context.OrderId is { } orderId)
            query = query.Where(r => r.OrderId != orderId);

        var totals = await query
            .GroupBy(r => r.PromotionId)
            .Select(g => new
            {
                PromotionId = g.Key,
                Count = g.Count(),
                Amount = g.Sum(r => r.DiscountAmount),
            })
            .ToListAsync(cancellationToken);

        var perCustomer = new Dictionary<int, int>();
        if (context.UserId is { } userId)
        {
            perCustomer = await query
                .Where(r => r.UserId == userId)
                .GroupBy(r => r.PromotionId)
                .Select(g => new { PromotionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PromotionId, x => x.Count, cancellationToken);
        }

        foreach (var total in totals)
            result[total.PromotionId] = new PromotionUsage(
                total.Count,
                total.Amount,
                perCustomer.GetValueOrDefault(total.PromotionId)
            );
        return result;
    }
}
