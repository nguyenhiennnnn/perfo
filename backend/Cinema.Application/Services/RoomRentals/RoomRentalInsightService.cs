using Cinema.Application.Caching;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalInsightService(
    IAppDbContext db,
    IAppCache cache,
    IPaymentLedgerService ledger
) : IRoomRentalInsightService
{
    private const int MaxRangeDays = 92;

    private static readonly RoomRentalRequestStatus[] ClosedStatuses =
    [
        RoomRentalRequestStatus.DepositPaid,
        RoomRentalRequestStatus.FullyPaid,
        RoomRentalRequestStatus.Completed,
    ];

    public async Task<List<RoomRentalCalendarItemDto>> GetCalendarAsync(
        DateOnly from,
        DateOnly to,
        int? roomId,
        CancellationToken cancellationToken
    )
    {
        var (fromUtc, toUtc) = await WindowAsync(from, to, cancellationToken);

        var query = db
            .Showtimes.AsNoTracking()
            .Where(s =>
                s.Kind == ShowtimeKind.Private
                && s.RoomRentalRequestId != null
                && s.Status != ShowtimeStatus.Cancelled
                && s.StartTime >= fromUtc
                && s.StartTime < toUtc
            );
        if (roomId is { } id)
            query = query.Where(s => s.RoomId == id);

        return await query
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.ShowtimeId)
            .Select(s => new RoomRentalCalendarItemDto(
                s.ShowtimeId,
                s.RoomId,
                s.Room.RoomName,
                s.StartTime,
                s.EndTime,
                s.ContentTitle,
                s.Status,
                s.RoomRentalRequestId!.Value,
                s.RoomRentalRequest!.RequestCode,
                s.RoomRentalRequest.OrganizationName ?? s.RoomRentalRequest.FullName,
                s.RoomRentalRequest.Status,
                s.RoomRentalRequest.AttendeeCount
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<RoomRentalCancelPreviewDto> GetCancelPreviewAsync(
        int requestId,
        bool venueIncident,
        CancellationToken cancellationToken
    )
    {
        var request =
            await db
                .RoomRentalRequests.AsNoTracking()
                .Where(r => r.RoomRentalRequestId == requestId)
                .Select(r => new { r.RequestCode, r.Status })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomRentalRequest);

        var kind = venueIncident ? RoomRentalCancelKind.Venue : RoomRentalCancelKind.Customer;
        var now = DateTime.UtcNow;

        var quote = RoomRentalCancellationRules.IsPaymentStage(request.Status)
            ? await db
                .RoomRentalQuotes.AsNoTracking()
                .Where(q => q.RoomRentalRequestId == requestId && q.SentAt != null)
                .OrderByDescending(q => q.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        if (quote is null)
            return new RoomRentalCancelPreviewDto(
                requestId,
                request.RequestCode,
                request.Status,
                false,
                kind,
                null,
                0,
                0,
                0m,
                0m,
                0m,
                0m
            );

        var orders = await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.RoomRentalRequestId == requestId
                && o.Type == OrderType.RoomRental
                && o.Status == OrderStatus.Confirmed
            )
            .Select(o => new { o.OrderId, o.RoomRentalPaymentStage })
            .ToListAsync(cancellationToken);

        var depositPaid = 0m;
        var balancePaid = 0m;
        foreach (var order in orders)
        {
            var paid = await ledger.GetNetPaidAmountAsync(order.OrderId, cancellationToken);
            if (order.RoomRentalPaymentStage == RoomRentalPaymentStage.Deposit)
                depositPaid += paid;
            else
                balancePaid += paid;
        }

        var percent = RoomRentalCancellationRules.DepositRefundPercent(kind, quote, now);
        var depositRefund = Math.Round(
            depositPaid * percent / 100m,
            0,
            MidpointRounding.AwayFromZero
        );
        var refund = depositRefund + balancePaid;

        return new RoomRentalCancelPreviewDto(
            requestId,
            request.RequestCode,
            request.Status,
            quote.StartAt > now,
            kind,
            quote.StartAt,
            Math.Round((quote.StartAt - now).TotalHours, 1),
            percent,
            depositPaid,
            balancePaid,
            refund,
            depositPaid + balancePaid - refund
        );
    }

    public async Task<RoomRentalReportDto> GetReportAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    )
    {
        var (fromUtc, toUtc) = await WindowAsync(from, to, cancellationToken);

        var requests = db
            .RoomRentalRequests.AsNoTracking()
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt < toUtc);

        var byStatus = await requests
            .GroupBy(r => r.Status)
            .Select(g => new RoomRentalReportStatusDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var quoted = await requests.CountAsync(
            r => db.RoomRentalQuotes.Any(q => q.RoomRentalRequestId == r.RoomRentalRequestId && q.SentAt != null),
            cancellationToken
        );
        var total = byStatus.Sum(s => s.Count);
        var closed = byStatus.Where(s => ClosedStatuses.Contains(s.Status)).Sum(s => s.Count);
        var cancelled = byStatus
            .Where(s => s.Status == RoomRentalRequestStatus.Cancelled)
            .Sum(s => s.Count);

        var daily = await db
            .RoomRentalRevenueEntries.AsNoTracking()
            .Where(e => e.BusinessDate >= from && e.BusinessDate <= to)
            .GroupBy(e => e.BusinessDate)
            .Select(g => new RoomRentalReportDailyDto(g.Key, g.Sum(e => e.Amount), g.Count()))
            .OrderBy(d => d.BusinessDate)
            .ToListAsync(cancellationToken);

        var pendingIds = await db
            .RoomRentalRequests.AsNoTracking()
            .Where(r =>
                (
                    r.Status == RoomRentalRequestStatus.DepositPaid
                    || r.Status == RoomRentalRequestStatus.FullyPaid
                )
                && !db.RoomRentalRevenueEntries.Any(e => e.RoomRentalRequestId == r.RoomRentalRequestId)
            )
            .Select(r => r.RoomRentalRequestId)
            .ToListAsync(cancellationToken);

        var pending = (
            await db
                .RoomRentalQuotes.AsNoTracking()
                .Where(q => pendingIds.Contains(q.RoomRentalRequestId) && q.SentAt != null)
                .Select(q => new { q.RoomRentalRequestId, q.VersionNumber, q.Total })
                .ToListAsync(cancellationToken)
        )
            .GroupBy(q => q.RoomRentalRequestId)
            .Sum(g => g.MaxBy(q => q.VersionNumber)!.Total);

        return new RoomRentalReportDto(
            from,
            to,
            total,
            quoted,
            closed,
            cancelled,
            quoted == 0 ? 0m : Math.Round(closed * 100m / quoted, 1),
            daily.Sum(d => d.Revenue),
            pending,
            byStatus.OrderBy(s => s.Status).ToList(),
            daily
        );
    }

    private async Task<(DateTime FromUtc, DateTime ToUtc)> WindowAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    )
    {
        if (to < from)
            throw new ValidationAppException(
                Messages.Validation_DateRangeTo_BeforeFrom,
                new Dictionary<string, string[]>
                {
                    ["to"] = [Messages.Validation_DateRangeTo_BeforeFrom],
                }
            );
        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
        {
            var message = string.Format(Messages.Validation_DateRange_TooLong, MaxRangeDays);
            throw new ValidationAppException(
                message,
                new Dictionary<string, string[]> { ["to"] = [message] }
            );
        }

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        return (
            BusinessDay.StartUtc(from, settings.OpeningTime, settings.ClosingTime),
            BusinessDay.EndUtc(to, settings.OpeningTime, settings.ClosingTime)
        );
    }
}
