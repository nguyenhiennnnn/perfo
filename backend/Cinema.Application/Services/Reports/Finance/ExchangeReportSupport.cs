using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

internal static class ExchangeReportSupport
{
    public static async Task<(DateTime FromUtc, DateTime ToUtc, TimeOnly? Opening, TimeOnly? Closing)> WindowAsync(
        IAppCache cache,
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        return (
            BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime),
            BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime),
            settings.OpeningTime,
            settings.ClosingTime
        );
    }

    public static IQueryable<OrderExchange> InWindow(
        IAppDbContext db,
        DateTime fromUtc,
        DateTime toUtc
    ) =>
        db.OrderExchanges.AsNoTracking()
            .Where(e => e.ExchangedAt >= fromUtc && e.ExchangedAt < toUtc);
}
