using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Bookings;

internal static class BookingAdminFilterExtensions
{
    public static IQueryable<Booking> ApplyAdminFilter(
        this IQueryable<Booking> query,
        AdminBookingListFilter filter
    )
    {
        if (
            !string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<BookingStatus>(filter.Status, out var st)
        )
            query = query.Where(b => b.Status == st);
        if (
            !string.IsNullOrWhiteSpace(filter.Channel)
            && Enum.TryParse<BookingChannel>(filter.Channel, out var ch)
        )
            query = query.Where(b => b.Channel == ch);
        if (filter.FromDate is not null)
            query = query.Where(b =>
                b.CreatedAt >= BusinessTime.StartOfDayUtc(filter.FromDate.Value)
            );
        if (filter.ToDate is not null)
            query = query.Where(b =>
                b.CreatedAt < BusinessTime.StartOfNextDayUtc(filter.ToDate.Value)
            );
        query = query.ApplySearch(filter.Search, (q, textPattern, phonePattern) =>
            q.Where(b =>
                EF.Functions.ILike(b.BookingCode, textPattern)
                || EF.Functions.ILike(EF.Functions.Unaccent(b.CustomerName), EF.Functions.Unaccent(textPattern))
                || (b.CustomerPhone != null
                    && (EF.Functions.ILike(b.CustomerPhone, textPattern)
                        || EF.Functions.ILike(b.CustomerPhone, phonePattern)))
            ));

        return query;
    }
}
