using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Promotions;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Promotions;

public class PromotionReportService(IAppDbContext db) : IPromotionReportService
{
    private const int RatioDecimals = 4;

    public async Task<PromotionReportListDto> GetListAsync(
        PromotionReportFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var promotions = ApplyFilter(db.Promotions.AsNoTracking(), filter);
        var total = await promotions.CountAsync(cancellationToken);
        var pageItems = await promotions
            .OrderByDescending(p => p.PromotionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rows = await LoadRedeemedAsync(
            db.PromotionRedemptions.Where(r =>
                promotions.Any(p => p.PromotionId == r.PromotionId)
            ),
            filter.FromDate,
            filter.ToDate,
            cancellationToken
        );

        var byPromotion = rows.ToLookup(x => x.PromotionId);
        var items = pageItems
            .Select(p =>
            {
                var own = byPromotion[p.PromotionId].ToList();
                return new PromotionReportListItemDto(
                    p.PromotionId,
                    p.Code,
                    p.Name,
                    p.Type.ToString(),
                    p.IsActive,
                    p.StartsAt,
                    p.EndsAt,
                    own.Count,
                    own.Sum(x => x.DiscountAmount),
                    own.Sum(x => x.NetRevenue),
                    p.TotalUsageLimit,
                    p.ConsumedCount,
                    Ratio(p.ConsumedCount, p.TotalUsageLimit),
                    p.BudgetAmount,
                    p.ConsumedBudget,
                    Ratio(p.ConsumedBudget, p.BudgetAmount)
                );
            })
            .ToList();

        var distinctOrders = rows.GroupBy(x => x.OrderId).Select(g => g.First()).ToList();
        var summary = new PromotionReportSummaryDto(
            total,
            rows.Count,
            distinctOrders.Count,
            rows.Sum(x => x.DiscountAmount),
            distinctOrders.Sum(x => x.NetRevenue)
        );

        return new PromotionReportListDto(
            summary,
            OffsetPageResult<PromotionReportListItemDto>.Create(items, page, pageSize, total)
        );
    }

    public async Task<PromotionReportDto> GetAsync(
        int id,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken
    )
    {
        var row =
            await db
                .Promotions.AsNoTracking()
                .Where(p => p.PromotionId == id)
                .Select(p => new PromotionRow(
                    p,
                    p.AssignedUser == null ? null : p.AssignedUser.FullName,
                    p.AssignedUser == null ? null : p.AssignedUser.Email
                ))
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Promotion);
        var promotion = row.Promotion;

        var rows = await LoadRedeemedAsync(
            db.PromotionRedemptions.Where(r => r.PromotionId == id),
            fromDate,
            toDate,
            cancellationToken
        );

        var activeHoldCount = await db.PromotionRedemptions.CountAsync(
            r => r.PromotionId == id && r.Status == PromotionRedemptionStatus.Reserved,
            cancellationToken
        );
        var released = db.PromotionRedemptions.Where(r =>
            r.PromotionId == id && r.Status == PromotionRedemptionStatus.Released
        );
        if (fromDate is { } fromDay)
        {
            var fromUtc = BusinessTime.StartOfDayUtc(fromDay);
            released = released.Where(r => r.ReleasedAt >= fromUtc);
        }

        if (toDate is { } toDay)
        {
            var toUtc = BusinessTime.StartOfNextDayUtc(toDay);
            released = released.Where(r => r.ReleasedAt < toUtc);
        }

        var releasedCount = await released.CountAsync(cancellationToken);

        var orderCount = rows.Select(x => x.OrderId).Distinct().Count();
        var totalDiscount = rows.Sum(x => x.DiscountAmount);
        var usage = new PromotionReportUsageDto(
            rows.Count,
            activeHoldCount,
            releasedCount,
            orderCount,
            rows.Where(x => x.UserId != null).Select(x => x.UserId).Distinct().Count(),
            totalDiscount,
            rows.Sum(x => x.NetRevenue),
            orderCount > 0
                ? Math.Round(totalDiscount / orderCount, 0, MidpointRounding.AwayFromZero)
                : 0m
        );

        var limits = new PromotionReportLimitsDto(
            promotion.TotalUsageLimit,
            promotion.ConsumedCount,
            promotion.TotalUsageLimit is { } limit
                ? Math.Max(0, limit - promotion.ConsumedCount)
                : null,
            Ratio(promotion.ConsumedCount, promotion.TotalUsageLimit),
            promotion.BudgetAmount,
            promotion.ConsumedBudget,
            promotion.BudgetAmount is { } budget
                ? Math.Max(0m, budget - promotion.ConsumedBudget)
                : null,
            Ratio(promotion.ConsumedBudget, promotion.BudgetAmount)
        );

        var byChannel = Enum.GetValues<SalesChannel>()
            .Select(channel =>
                Breakdown(channel.ToString(), rows.Where(x => x.Channel == channel).ToList())
            )
            .ToList();
        var byTimeSlot = PromotionTimeSlots
            .All.Select(slot =>
                Breakdown(
                    slot,
                    rows.Where(x =>
                            PromotionTimeSlots.FromHour(BusinessTime.ToLocal(x.RedeemedAt).Hour)
                            == slot
                        )
                        .ToList()
                )
            )
            .ToList();

        return new PromotionReportDto(
            PromotionMapper.ToDto(row),
            usage,
            limits,
            byChannel,
            byTimeSlot
        );
    }

    private static IQueryable<Promotion> ApplyFilter(
        IQueryable<Promotion> query,
        PromotionReportFilter filter
    )
    {
        if (filter.IsActive is { } isActive)
            query = query.Where(p => p.IsActive == isActive);
        if (EnumConverters.ParseNullableEnum<PromotionType>(filter.Type) is { } type)
            query = query.Where(p => p.Type == type);
        return query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(p => EF.Functions.ILike(p.Code, pattern) || EF.Functions.ILike(p.Name, pattern))
        );
    }

    private async Task<List<RedeemedRow>> LoadRedeemedAsync(
        IQueryable<PromotionRedemption> redemptions,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken
    )
    {
        var query = redemptions.Where(r =>
            r.Status == PromotionRedemptionStatus.Redeemed && r.RedeemedAt != null
        );
        if (fromDate is { } fromDay)
        {
            var fromUtc = BusinessTime.StartOfDayUtc(fromDay);
            query = query.Where(r => r.RedeemedAt >= fromUtc);
        }

        if (toDate is { } toDay)
        {
            var toUtc = BusinessTime.StartOfNextDayUtc(toDay);
            query = query.Where(r => r.RedeemedAt < toUtc);
        }

        var rows = await query
            .AsNoTracking()
            .Select(r => new
            {
                r.PromotionId,
                r.OrderId,
                r.UserId,
                r.Order.Channel,
                RedeemedAt = r.RedeemedAt!.Value,
                r.DiscountAmount,
                r.Order.FinalAmount,
            })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return [];

        var orderIds = rows.Select(x => x.OrderId).Distinct().ToList();
        var refunds = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.Type == PaymentTransactionType.Refund
                && t.Status == PaymentStatus.Success
                && orderIds.Contains(t.OrderId)
            )
            .GroupBy(t => t.OrderId)
            .Select(g => new { OrderId = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.OrderId, x => x.Amount, cancellationToken);

        return rows.Select(x => new RedeemedRow(
                x.PromotionId,
                x.OrderId,
                x.UserId,
                x.Channel,
                x.RedeemedAt,
                x.DiscountAmount,
                ChargeRevenueQuery.NetOf(x.FinalAmount, refunds.GetValueOrDefault(x.OrderId))
            ))
            .ToList();
    }

    private static PromotionReportBreakdownItemDto Breakdown(
        string key,
        List<RedeemedRow> rows
    ) => new(key, rows.Count, rows.Sum(x => x.DiscountAmount), rows.Sum(x => x.NetRevenue));

    private static decimal? Ratio(decimal consumed, decimal? limit) =>
        limit is > 0m ? Math.Round(consumed / limit.Value, RatioDecimals) : null;

    private static decimal? Ratio(int consumed, int? limit) =>
        limit is > 0 ? Math.Round((decimal)consumed / limit.Value, RatioDecimals) : null;

    private sealed record RedeemedRow(
        int PromotionId,
        int OrderId,
        int? UserId,
        SalesChannel Channel,
        DateTime RedeemedAt,
        decimal DiscountAmount,
        decimal NetRevenue
    );
}
