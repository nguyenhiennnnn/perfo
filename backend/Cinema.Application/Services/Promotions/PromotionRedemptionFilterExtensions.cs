using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Promotions;

internal static class PromotionRedemptionFilterExtensions
{
    public static IQueryable<PromotionRedemption> ApplyFilter(
        this IQueryable<PromotionRedemption> query,
        PromotionRedemptionListFilter filter
    )
    {
        var promotionId = filter.PromotionId;
        query = query.Where(r => r.PromotionId == promotionId);

        if (EnumConverters.TryParseEnum<PromotionRedemptionStatus>(filter.Status, out var status))
            query = query.Where(r => r.Status == status);
        if (EnumConverters.TryParseEnum<SalesChannel>(filter.Channel, out var channel))
            query = query.Where(r => r.Order.Channel == channel);
        if (filter.FromDate is { } fromDate)
        {
            var fromUtc = BusinessTime.StartOfDayUtc(fromDate);
            query = query.Where(r => r.ReservedAt >= fromUtc);
        }

        if (filter.ToDate is { } toDate)
        {
            var toUtc = BusinessTime.StartOfNextDayUtc(toDate);
            query = query.Where(r => r.ReservedAt < toUtc);
        }

        return query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(r =>
                    EF.Functions.ILike(r.Order.OrderCode, pattern)
                    || (
                        r.Order.CustomerName != null
                        && EF.Functions.ILike(
                            EF.Functions.Unaccent(r.Order.CustomerName),
                            EF.Functions.Unaccent(pattern)
                        )
                    )
                    || (
                        r.User != null
                        && EF.Functions.ILike(
                            EF.Functions.Unaccent(r.User.FullName),
                            EF.Functions.Unaccent(pattern)
                        )
                    )
                )
        );
    }
}
