using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Payments;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Payments;

public class CashRefundService(IAppDbContext db, IPaymentRefundService paymentRefunds)
    : ICashRefundService
{
    public async Task<OffsetPageResult<CashRefundListItemDto>> GetCashRefundsAsync(
        CashRefundListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var query = BaseQuery();

        if (string.Equals(filter.Status, CashRefundStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            query = query.Where(t => t.Status == PaymentStatus.Pending);
        else if (
            string.Equals(filter.Status, CashRefundStatuses.Refunded, StringComparison.OrdinalIgnoreCase)
        )
            query = query.Where(t => t.Status == PaymentStatus.Success);

        if (!string.IsNullOrWhiteSpace(filter.OrderCode))
        {
            var pattern = SearchHelper.ILikePattern(filter.OrderCode.Trim());
            query = query.Where(t => EF.Functions.ILike(t.Order.OrderCode, pattern));
        }

        if (filter.FromDate is not null)
            query = query.Where(t =>
                t.CreatedAt >= DateTimeUtils.GetStartOfDay(filter.FromDate.Value)
            );
        if (filter.ToDate is not null)
            query = query.Where(t =>
                t.CreatedAt < DateTimeUtils.GetStartOfNextDay(filter.ToDate.Value)
            );

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(t => t.Status == PaymentStatus.Pending ? 0 : 1)
            .ThenByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.PaymentTransactionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.PaymentTransactionId,
                t.TransactionCode,
                t.OrderId,
                t.Order.OrderCode,
                t.Order.OriginalOrderId,
                t.Order.UserId,
                UserFullName = t.Order.User != null ? t.Order.User.FullName : null,
                t.Order.CustomerName,
                t.Order.CustomerPhone,
                t.Amount,
                t.Status,
                t.CreatedAt,
                t.CompletedAt,
                t.PerformedByUserId,
                ConfirmedByName = t.PerformedByUser != null ? t.PerformedByUser.FullName : null,
            })
            .ToListAsync(cancellationToken);

        var showtimes = await LoadShowtimesAsync(
            [
                .. rows
                    .SelectMany(r => new[] { r.OrderId, r.OriginalOrderId ?? r.OrderId })
                    .Distinct(),
            ],
            cancellationToken
        );

        var items = rows.Select(r =>
            {
                showtimes.TryGetValue(r.OrderId, out var showtime);
                if (showtime is null && r.OriginalOrderId is not null)
                    showtimes.TryGetValue(r.OriginalOrderId.Value, out showtime);
                var refunded = r.Status == PaymentStatus.Success;
                return new CashRefundListItemDto(
                    r.PaymentTransactionId,
                    r.TransactionCode,
                    r.OrderId,
                    r.OrderCode,
                    r.UserId,
                    r.CustomerName ?? r.UserFullName ?? string.Empty,
                    r.CustomerPhone,
                    r.UserId is null,
                    r.Amount,
                    showtime?.ShowtimeId,
                    showtime?.MovieTitle,
                    showtime?.StartTime,
                    refunded ? CashRefundStatuses.Refunded : CashRefundStatuses.Pending,
                    r.CreatedAt,
                    refunded ? r.CompletedAt : null,
                    refunded ? r.PerformedByUserId : null,
                    refunded ? r.ConfirmedByName : null
                );
            })
            .ToList();

        return OffsetPageResult<CashRefundListItemDto>.Create(items, page, pageSize, total);
    }

    public async Task<CashRefundConfirmResultDto> ConfirmAsync(
        int refundTransactionId,
        int confirmedByUserId,
        CancellationToken cancellationToken
    )
    {
        await EnsureTrackedCashRefundAsync(refundTransactionId, cancellationToken);
        await paymentRefunds.ConfirmCashRefundAsync(
            refundTransactionId,
            confirmedByUserId,
            cancellationToken
        );

        var refund = await BaseQuery()
            .Where(t => t.PaymentTransactionId == refundTransactionId)
            .Select(t => new CashRefundConfirmResultDto(
                t.PaymentTransactionId,
                t.OrderId,
                t.Order.OrderCode,
                t.Amount,
                t.Status == PaymentStatus.Success
                    ? CashRefundStatuses.Refunded
                    : CashRefundStatuses.Pending,
                t.Status == PaymentStatus.Success ? t.CompletedAt : null,
                t.Status == PaymentStatus.Success ? t.PerformedByUserId : null,
                t.Status == PaymentStatus.Success && t.PerformedByUser != null
                    ? t.PerformedByUser.FullName
                    : null
            ))
            .FirstOrDefaultAsync(cancellationToken);
        return refund ?? throw new NotFoundException(Messages.NotFound_Payment);
    }

    private async Task EnsureTrackedCashRefundAsync(
        int refundTransactionId,
        CancellationToken cancellationToken
    )
    {
        if (!await BaseQuery().AnyAsync(t => t.PaymentTransactionId == refundTransactionId, cancellationToken))
            throw new NotFoundException(Messages.NotFound_Payment);
    }

    private IQueryable<PaymentTransaction> BaseQuery() =>
        db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.Type == PaymentTransactionType.Refund
                && t.Method == PaymentMethod.Cash
                && t.Status != PaymentStatus.Failed
                && (
                    t.Order.CancelReason == CancelReason.ShowtimeCancelled
                    || t.Order.CancelReason == CancelReason.RoomClosed
                )
            );

    private async Task<Dictionary<int, ShowtimeBrief>> LoadShowtimesAsync(
        List<int> orderIds,
        CancellationToken cancellationToken
    ) =>
        (
            await db
                .OrderLines.AsNoTracking()
                .Where(l =>
                    orderIds.Contains(l.OrderId)
                    && l.LineType == OrderLineType.Ticket
                    && l.ShowtimeId != null
                )
                .Select(l => new
                {
                    l.OrderId,
                    ShowtimeId = l.ShowtimeId!.Value,
                    l.Showtime!.Movie.Title,
                    l.Showtime.StartTime,
                })
                .ToListAsync(cancellationToken)
        )
            .GroupBy(l => l.OrderId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var first = g.OrderBy(x => x.StartTime).First();
                    return new ShowtimeBrief(first.ShowtimeId, first.Title, first.StartTime);
                }
            );

    private sealed record ShowtimeBrief(int ShowtimeId, string MovieTitle, DateTime StartTime);
}
