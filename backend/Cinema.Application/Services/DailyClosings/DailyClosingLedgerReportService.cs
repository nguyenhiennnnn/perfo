using System.Text.Json;
using System.Text.Json.Serialization;
using Cinema.Application.DTOs.DailyClosings;
using Cinema.Application.Interfaces.DailyClosings;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.DailyClosings;

public class DailyClosingLedgerReportService(
    IAppDbContext db,
    IDailyClosingReportService reportService
) : IDailyClosingLedgerReportService
{
    private static readonly JsonSerializerOptions SnapshotOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<DailyClosingReportDto> GetAsync(
        DateOnly businessDate,
        DailyClosingReportMode mode,
        CancellationToken cancellationToken
    )
    {
        var row = await db
            .DailyClosings.AsNoTracking()
            .Where(c => c.BusinessDate == businessDate && c.Status == DailyClosingStatus.Closed)
            .Select(c => new { Closing = c, ClosedByName = c.ClosedByUser!.FullName })
            .FirstOrDefaultAsync(cancellationToken);

        DailyClosingSnapshot snapshot;
        DailyClosingListItemDto? closing = null;
        if (row is not null)
        {
            snapshot =
                JsonSerializer.Deserialize<DailyClosingSnapshot>(
                    row.Closing.SnapshotJson ?? "{}",
                    SnapshotOptions
                ) ?? throw new NotFoundException(Messages.NotFound_DailyClosing);
            closing = DailyClosingService.ToItem(row.Closing, row.ClosedByName);
        }
        else
            snapshot = await reportService.BuildDraftAsync(businessDate, cancellationToken);

        var query = db.LedgerAdjustments.AsNoTracking();
        query =
            mode == DailyClosingReportMode.PostingDate
                ? query.Where(a => a.PostingDate == businessDate)
                : query.Where(a => a.OriginalDate == businessDate);
        var adjustments = await query
            .OrderBy(a => a.PostingDate)
            .ThenBy(a => a.LedgerAdjustmentId)
            .ToListAsync(cancellationToken);

        var totals = new DailyClosingAdjustmentTotalsDto(
            adjustments.Count,
            adjustments.Sum(a => a.TicketRevenueDelta),
            adjustments.Sum(a => a.ComboRevenueDelta),
            adjustments.Sum(a => a.GiftCardSoldDelta),
            adjustments.Sum(a => a.DiscountDelta),
            adjustments.Sum(a => a.RefundAmountDelta),
            adjustments.Sum(a => a.CancelledOrdersDelta)
        );
        var baseFigures = new DailyClosingFiguresDto(
            snapshot.Revenue.TicketRevenue,
            snapshot.Revenue.ComboRevenue,
            snapshot.Revenue.TotalRevenue,
            snapshot.Revenue.GiftCardSold,
            snapshot.Revenue.GiftCardUsed,
            snapshot.Discounts.TotalDiscount,
            snapshot.Refunds.TicketRefundAmount,
            snapshot.Refunds.CancelledOrders
        );
        var ticket = baseFigures.TicketRevenue + totals.TicketRevenue;
        var combo = baseFigures.ComboRevenue + totals.ComboRevenue;
        var adjusted = new DailyClosingFiguresDto(
            ticket,
            combo,
            ticket + combo,
            baseFigures.GiftCardSold + totals.GiftCardSold,
            baseFigures.GiftCardUsed,
            baseFigures.TotalDiscount + totals.Discount,
            baseFigures.TicketRefundAmount + totals.RefundAmount,
            baseFigures.CancelledOrders + totals.CancelledOrders
        );

        return new DailyClosingReportDto(
            mode.ToString(),
            businessDate,
            closing is not null,
            closing,
            snapshot,
            baseFigures,
            adjustments.Select(ToDto).ToList(),
            totals,
            adjusted
        );
    }

    private static DailyClosingAdjustmentDto ToDto(LedgerAdjustment a) =>
        new(
            a.LedgerAdjustmentId,
            a.PostingDate,
            a.OriginalDate,
            a.Type.ToString(),
            a.TicketRevenueDelta,
            a.ComboRevenueDelta,
            a.GiftCardSoldDelta,
            a.DiscountDelta,
            a.RefundAmountDelta,
            a.CancelledOrdersDelta,
            a.Reason,
            a.OrderId,
            a.PaymentTransactionId,
            a.CreatedAt
        );
}
