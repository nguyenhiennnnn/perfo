using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Application.Interfaces.Promotions;
using Cinema.Application.Services.Pricing;
using Cinema.Application.Services.Promotions;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OrderPriceCalculator(
    IAppDbContext db,
    IAppCache cache,
    IPromotionEngine promotionEngine,
    IMembershipBenefitPricer membershipBenefits
) : IOrderPriceCalculator
{
    private static readonly PriceAdjustmentType[] RecalculatedTypes =
    [
        PriceAdjustmentType.TierDiscount,
        PriceAdjustmentType.MembershipBenefit,
        PriceAdjustmentType.Promotion,
    ];

    public async Task<OrderPriceResult> ApplyAsync(
        Order order,
        OrderPricingOptions options,
        CancellationToken cancellationToken
    )
    {
        ClearAdjustments(order);

        if (order.Type is not (OrderType.Ticket or OrderType.Combo))
        {
            OrderPricing.ApplyTotals(order);
            return OrderPriceResult.None;
        }

        var lines = order.Lines.ToList();
        var showtimeIds = lines
            .Where(l => l.ShowtimeId != null)
            .Select(l => l.ShowtimeId!.Value)
            .Distinct()
            .ToList();
        if (
            showtimeIds.Count > 0
            && await db.Showtimes.AnyAsync(
                s => showtimeIds.Contains(s.ShowtimeId) && s.Kind == ShowtimeKind.Private,
                cancellationToken
            )
        )
        {
            OrderPricing.ApplyTotals(order);
            return OrderPriceResult.None;
        }

        var remaining = lines.Select(l => l.LockedUnitPrice * l.Quantity).ToArray();
        foreach (
            var allocation in order
                .PriceAdjustments.Where(a =>
                    a.Type is PriceAdjustmentType.PackagePrice or PriceAdjustmentType.FreeTicket
                )
                .SelectMany(a => a.Allocations)
        )
        {
            var index = lines.IndexOf(allocation.OrderLine);
            if (index >= 0)
                remaining[index] -= allocation.Amount;
        }
        var hasTickets = lines.Any(l => l.LineType == OrderLineType.Ticket);
        var wantsPromotions =
            options.ApplyAutoPromotions || !string.IsNullOrWhiteSpace(options.PromotionCode);

        var tier =
            hasTickets || wantsPromotions
                ? await ResolveTierAsync(order.UserId, cancellationToken)
                : null;

        if (tier is not null && hasTickets)
        {
            var ticketBases = lines
                .Select((l, i) => l.LineType == OrderLineType.Ticket ? remaining[i] : 0m)
                .ToArray();
            var amount = OrderPricing.TierDiscountAmount(ticketBases.Sum(), tier.DiscountPercent);
            if (amount > 0m)
                AddAdjustment(
                    order,
                    lines,
                    remaining,
                    PriceAdjustmentType.TierDiscount,
                    nameof(MembershipTier),
                    tier.TierId,
                    DiscountAllocator.Allocate(amount, ticketBases)
                );
        }

        var benefits = await membershipBenefits.ComputeAsync(
            new MembershipBenefitContext(order, lines, remaining),
            cancellationToken
        );
        var benefitExcludesPromotions = false;
        foreach (var benefit in benefits)
        {
            var shares = benefit
                .LineShares.Select((share, i) => Math.Max(Math.Min(share, remaining[i]), 0m))
                .ToArray();
            if (shares.Sum() <= 0m)
                continue;

            AddAdjustment(
                order,
                lines,
                remaining,
                PriceAdjustmentType.MembershipBenefit,
                benefit.ReferenceType,
                benefit.ReferenceId,
                shares
            );
            benefitExcludesPromotions |= benefit.ExcludesPromotions;
        }

        if (
            wantsPromotions
            && (benefitExcludesPromotions || await HasExcludingBundleAsync(order, cancellationToken))
        )
        {
            OrderPricing.ApplyTotals(order);
            return string.IsNullOrWhiteSpace(options.PromotionCode)
                ? OrderPriceResult.None
                : new OrderPriceResult(
                    PromotionRejections.Invalid(
                        benefitExcludesPromotions
                            ? PromotionRejectionReason.NotCombinable
                            : PromotionRejectionReason.BundleExcluded
                    ),
                    []
                );
        }

        PromotionResolution? resolution = null;
        if (wantsPromotions)
        {
            var context = await BuildPromotionContextAsync(
                order,
                lines,
                remaining,
                tier?.TierId,
                cancellationToken
            );
            resolution = await promotionEngine.ResolveAsync(
                context,
                options.PromotionCode,
                options.ApplyAutoPromotions,
                cancellationToken
            );
            foreach (var applied in resolution.Applied)
                AddAdjustment(
                    order,
                    lines,
                    remaining,
                    PriceAdjustmentType.Promotion,
                    nameof(Promotion),
                    applied.Promotion.PromotionId,
                    applied.LineShares.ToArray()
                );
        }

        OrderPricing.ApplyTotals(order);
        return resolution is null
            ? OrderPriceResult.None
            : new OrderPriceResult(resolution.CodeResult, resolution.Applied);
    }

    private async Task<bool> HasExcludingBundleAsync(
        Order order,
        CancellationToken cancellationToken
    )
    {
        var bundleIds = order
            .PriceAdjustments.Where(a =>
                a.Type == PriceAdjustmentType.PackagePrice && a.ReferenceId != null
            )
            .Select(a => a.ReferenceId!.Value)
            .Distinct()
            .ToList();
        return bundleIds.Count > 0
            && await db
                .Bundles.IgnoreQueryFilters()
                .AnyAsync(
                    b => bundleIds.Contains(b.BundleId) && b.ExcludesPromotions,
                    cancellationToken
                );
    }

    private void ClearAdjustments(Order order)
    {
        foreach (
            var adjustment in order
                .PriceAdjustments.Where(a => RecalculatedTypes.Contains(a.Type))
                .ToList()
        )
        {
            order.PriceAdjustments.Remove(adjustment);
            if (adjustment.AdjustmentId != 0)
                db.OrderPriceAdjustments.Remove(adjustment);
        }
    }

    private static void AddAdjustment(
        Order order,
        IReadOnlyList<OrderLine> lines,
        decimal[] remaining,
        PriceAdjustmentType type,
        string? referenceType,
        int? referenceId,
        decimal[] shares
    )
    {
        order.PriceAdjustments.Add(
            OrderPricing.BuildAdjustment(type, referenceType, referenceId, lines, shares)
        );
        for (var i = 0; i < remaining.Length; i++)
            remaining[i] -= shares[i];
    }

    private async Task<MembershipTierSnapshot?> ResolveTierAsync(
        int? userId,
        CancellationToken cancellationToken
    )
    {
        if (userId is null)
            return null;

        var hasProfile = await db.CustomerProfiles.AnyAsync(
            p => p.UserId == userId,
            cancellationToken
        );
        if (!hasProfile)
            return null;

        var points = await LoyaltyWindow.SumActiveAsync(
            db,
            userId.Value,
            DateTime.UtcNow,
            cancellationToken
        );
        var tiers = await cache.GetAsync(CacheKeys.MembershipTiers, cancellationToken);
        return LoyaltyWindow.ResolveTier(tiers, points);
    }

    private async Task<PromotionContext> BuildPromotionContextAsync(
        Order order,
        IReadOnlyList<OrderLine> lines,
        decimal[] remaining,
        int? tierId,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var showtimeId = lines.Select(l => l.ShowtimeId).FirstOrDefault(x => x != null);
        int? movieId = null;
        int? screenTypeId = null;
        var businessDate = BusinessTime.LocalDate(now);

        if (showtimeId is { } id)
        {
            var showtime = await db
                .Showtimes.AsNoTracking()
                .Where(s => s.ShowtimeId == id)
                .Select(s => new
                {
                    s.MovieId,
                    s.StartTime,
                    s.Room.ScreenTypeId,
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (showtime is not null)
            {
                var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
                movieId = showtime.MovieId;
                screenTypeId = showtime.ScreenTypeId;
                businessDate = PricingCalendar.BusinessDate(
                    BusinessTime.ToLocal(showtime.StartTime),
                    settings.OpeningTime,
                    settings.ClosingTime
                );
            }
        }

        return new PromotionContext(
            order.OrderId == 0 ? null : order.OrderId,
            order.Channel,
            order.Type,
            order.UserId,
            tierId,
            showtimeId,
            movieId,
            screenTypeId,
            now,
            businessDate,
            lines.Select((l, i) => new PromotionLine(l.LineType, remaining[i])).ToList()
        );
    }
}
