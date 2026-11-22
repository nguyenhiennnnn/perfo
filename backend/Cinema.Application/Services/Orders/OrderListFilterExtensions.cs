using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

internal static class OrderListFilterExtensions
{
    public static IQueryable<Order> ApplyFilter(
        this IQueryable<Order> query,
        OrderListFilter filter
    )
    {
        if (EnumConverters.TryParseEnum<OrderStatus>(filter.Status, out var status))
            query = query.Where(o => o.Status == status);
        if (EnumConverters.TryParseEnum<SalesChannel>(filter.Channel, out var channel))
            query = query.Where(o => o.Channel == channel);
        if (EnumConverters.TryParseEnum<OrderType>(filter.Type, out var type))
            query = query.Where(o => o.Type == type);
        if (filter.FromDate is not null)
            query = query.Where(o =>
                o.CreatedAt >= BusinessTime.StartOfDayUtc(filter.FromDate.Value)
            );
        if (filter.ToDate is not null)
            query = query.Where(o =>
                o.CreatedAt < BusinessTime.StartOfNextDayUtc(filter.ToDate.Value)
            );

        return query.ApplySearch(
            filter.Search,
            (q, textPattern, phonePattern) =>
                q.Where(o =>
                    EF.Functions.ILike(o.OrderCode, textPattern)
                    || (
                        o.CustomerName != null
                        && EF.Functions.ILike(
                            EF.Functions.Unaccent(o.CustomerName),
                            EF.Functions.Unaccent(textPattern)
                        )
                    )
                    || (
                        o.CustomerPhone != null
                        && (
                            EF.Functions.ILike(o.CustomerPhone, textPattern)
                            || EF.Functions.ILike(o.CustomerPhone, phonePattern)
                        )
                    )
                )
        );
    }
}
