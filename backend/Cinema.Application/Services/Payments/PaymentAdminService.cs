using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Payments;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Payments;

public class PaymentAdminService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IOptions<BookingOptions> bookingOptions
) : IPaymentAdminService
{
    private readonly BookingOptions _options = bookingOptions.Value;

    public async Task<OffsetPageResult<AdminPaymentListItemDto>> GetPaymentsAsync(
        AdminPaymentListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(1, page);
        pageSize =
            pageSize <= 0 ? _options.DefaultPageSize : Math.Min(pageSize, _options.MaxPageSize);

        var query = db.PaymentTransactions.AsNoTracking().ApplyAdminFilter(filter);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.PaymentTransactionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.PaymentTransactionId,
                t.TransactionCode,
                t.OrderId,
                t.Order.OrderCode,
                t.Type,
                t.Method,
                t.Status,
                t.Amount,
                t.GatewayFee,
                t.GatewayTransactionId,
                t.GatewayResponseCode,
                t.AmountReceived,
                t.ChangeGiven,
                t.OriginalTransactionId,
                t.NeedsManualRefund,
                t.CreatedAt,
                t.CompletedAt,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new AdminPaymentListItemDto(
                r.PaymentTransactionId,
                r.TransactionCode,
                r.OrderId,
                r.OrderCode,
                r.Type.ToString(),
                r.Method.ToDtoMethod(),
                r.Status.ToDtoStatus(),
                r.Amount,
                r.GatewayFee,
                r.GatewayTransactionId,
                r.GatewayResponseCode,
                r.AmountReceived,
                r.ChangeGiven,
                r.OriginalTransactionId,
                r.NeedsManualRefund,
                r.CreatedAt,
                r.CompletedAt
            ))
            .ToList();

        return OffsetPageResult<AdminPaymentListItemDto>.Create(items, page, pageSize, total);
    }

    public async Task<AdminPaymentDetailDto> GetPaymentDetailAsync(
        int transactionId,
        CancellationToken cancellationToken
    )
    {
        var transaction =
            await db
                .PaymentTransactions.AsNoTracking()
                .Include(t => t.PerformedByUser)
                .FirstOrDefaultAsync(t => t.PaymentTransactionId == transactionId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Payment);

        var chargeId =
            transaction.Type == PaymentTransactionType.Charge
                ? transaction.PaymentTransactionId
                : transaction.OriginalTransactionId!.Value;

        var original =
            transaction.Type == PaymentTransactionType.Refund
                ? await db
                    .PaymentTransactions.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.PaymentTransactionId == chargeId, cancellationToken)
                : null;

        var refunds = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OriginalTransactionId == chargeId
                && t.PaymentTransactionId != transactionId
            )
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.PaymentTransactionId)
            .ToListAsync(cancellationToken);

        var charge = original ?? (transaction.Type == PaymentTransactionType.Charge ? transaction : null);
        var summary = charge is null
            ? null
            : await BuildRefundSummaryAsync(charge, transaction, refunds, cancellationToken);

        return new AdminPaymentDetailDto(
            transaction.PaymentTransactionId,
            transaction.TransactionCode,
            transaction.OrderId,
            transaction.Type.ToString(),
            transaction.Method.ToDtoMethod(),
            transaction.Status.ToDtoStatus(),
            transaction.Amount,
            transaction.GatewayFee,
            transaction.GatewayTransactionId,
            transaction.GatewayResponseCode,
            transaction.AmountReceived,
            transaction.ChangeGiven,
            transaction.NeedsManualRefund,
            transaction.Note,
            transaction.CreatedAt,
            transaction.CompletedAt,
            transaction.PerformedByUserId,
            transaction.PerformedByUser?.FullName,
            original is null ? null : ToRelated(original),
            refunds.Select(ToRelated).ToList(),
            summary,
            await LoadOrderBriefAsync(transaction.OrderId, cancellationToken)
        );
    }

    private async Task<AdminPaymentRefundSummaryDto> BuildRefundSummaryAsync(
        PaymentTransaction charge,
        PaymentTransaction current,
        List<PaymentTransaction> otherRefunds,
        CancellationToken cancellationToken
    )
    {
        var refunds = current.Type == PaymentTransactionType.Refund
            ? otherRefunds.Append(current)
            : otherRefunds;
        var refundList = refunds.ToList();

        return new AdminPaymentRefundSummaryDto(
            charge.PaymentTransactionId,
            charge.Amount,
            refundList
                .Where(r => r.Status == PaymentStatus.Success)
                .Sum(r => r.Amount),
            refundList
                .Where(r => r.Status == PaymentStatus.Pending)
                .Sum(r => r.Amount),
            await ledger.GetRefundableAmountAsync(charge.PaymentTransactionId, cancellationToken)
        );
    }

    private async Task<AdminPaymentOrderBriefDto> LoadOrderBriefAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var order =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderCode,
                    o.Type,
                    o.Channel,
                    o.Status,
                    o.UserId,
                    UserFullName = o.User != null ? o.User.FullName : null,
                    o.CustomerName,
                    o.CustomerPhone,
                    o.FinalAmount,
                    o.CreatedAt,
                    o.CancelReason,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        var tickets = await db
            .OrderLines.AsNoTracking()
            .Where(l => l.OrderId == orderId && l.LineType == OrderLineType.Ticket)
            .OrderBy(l => l.OrderLineId)
            .Select(l => new
            {
                l.Quantity,
                MovieTitle = l.Showtime != null ? l.Showtime.Movie.Title : null,
                StartTime = l.Showtime != null ? (DateTime?)l.Showtime.StartTime : null,
            })
            .ToListAsync(cancellationToken);
        var firstShowtime = tickets.FirstOrDefault(t => t.MovieTitle is not null);

        return new AdminPaymentOrderBriefDto(
            order.OrderId,
            order.OrderCode,
            order.Type.ToString(),
            order.Channel.ToDtoChannel(),
            order.Status.ToDtoStatus(),
            order.UserId,
            order.CustomerName ?? order.UserFullName,
            order.CustomerPhone,
            firstShowtime?.MovieTitle,
            firstShowtime?.StartTime,
            tickets.Sum(t => t.Quantity),
            order.FinalAmount,
            order.CreatedAt,
            order.CancelReason?.ToString()
        );
    }

    private static AdminPaymentRelatedTransactionDto ToRelated(PaymentTransaction t) =>
        new(
            t.PaymentTransactionId,
            t.TransactionCode,
            t.Type.ToString(),
            t.Method.ToDtoMethod(),
            t.Status.ToDtoStatus(),
            t.Amount,
            t.NeedsManualRefund,
            t.CreatedAt,
            t.CompletedAt
        );
}
