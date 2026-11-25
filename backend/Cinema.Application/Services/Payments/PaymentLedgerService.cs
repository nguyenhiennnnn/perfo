using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Payments;

public class PaymentLedgerService(IAppDbContext db) : IPaymentLedgerService
{
    public async Task<OrderPaymentSummary> GetSummaryAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var finalAmount =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => (decimal?)o.FinalAmount)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        var totals = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t => t.OrderId == orderId && t.Status != PaymentStatus.Failed)
            .GroupBy(t => new { t.Type, t.Status })
            .Select(g => new
            {
                g.Key.Type,
                g.Key.Status,
                Total = g.Sum(x => x.Amount),
            })
            .ToListAsync(cancellationToken);

        decimal Sum(PaymentTransactionType type, PaymentStatus status) =>
            totals.Where(x => x.Type == type && x.Status == status).Sum(x => x.Total);

        var collected = Sum(PaymentTransactionType.Charge, PaymentStatus.Success);
        var refunded = Sum(PaymentTransactionType.Refund, PaymentStatus.Success);
        var pendingRefund = Sum(PaymentTransactionType.Refund, PaymentStatus.Pending);
        var netPaid = collected - refunded;

        return new OrderPaymentSummary(
            orderId,
            finalAmount,
            collected,
            refunded,
            pendingRefund,
            netPaid,
            Math.Max(0m, finalAmount - netPaid),
            Math.Max(0m, netPaid - pendingRefund - finalAmount),
            netPaid >= finalAmount
        );
    }

    public async Task<decimal> GetNetPaidAmountAsync(
        int orderId,
        CancellationToken cancellationToken
    ) => (await GetSummaryAsync(orderId, cancellationToken)).NetPaidAmount;

    public async Task<decimal> GetRefundedAmountAsync(
        int orderId,
        CancellationToken cancellationToken
    ) => (await GetSummaryAsync(orderId, cancellationToken)).RefundedAmount;

    public async Task<bool> IsFullyPaidAsync(int orderId, CancellationToken cancellationToken) =>
        (await GetSummaryAsync(orderId, cancellationToken)).IsFullyPaid;

    public async Task<decimal> GetRefundableAmountAsync(
        int chargeTransactionId,
        CancellationToken cancellationToken
    )
    {
        var charge =
            await db
                .PaymentTransactions.AsNoTracking()
                .Where(t => t.PaymentTransactionId == chargeTransactionId)
                .Select(t => new
                {
                    t.Type,
                    t.Status,
                    t.Amount,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Payment);

        if (charge.Type != PaymentTransactionType.Charge || charge.Status != PaymentStatus.Success)
            return 0m;

        var reserved = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OriginalTransactionId == chargeTransactionId
                && t.Type == PaymentTransactionType.Refund
                && t.Status != PaymentStatus.Failed
            )
            .SumAsync(t => (decimal?)t.Amount, cancellationToken);

        return Math.Max(0m, charge.Amount - (reserved ?? 0m));
    }
}
