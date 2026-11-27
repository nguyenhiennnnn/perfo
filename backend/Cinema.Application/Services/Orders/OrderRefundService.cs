using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderRefundService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IPaymentRefundService refundService,
    IBusinessEventDispatcher eventDispatcher,
    ILogger<OrderRefundService> logger,
    IGiftCardRefundHandler? giftCardHandler = null
) : IOrderRefundService
{
    public async Task<OrderRefundPlan> BuildPlanAsync(
        int orderId,
        RefundScope scope,
        CancellationToken cancellationToken
    )
    {
        var charges = await LoadChargeCapacitiesAsync(orderId, cancellationToken);
        var combos = await db
            .OrderLines.AsNoTracking()
            .Where(l => l.OrderId == orderId && l.LineType == OrderLineType.Combo)
            .Select(l => new RefundComboShare(l.PaidAmount, l.Quantity, l.QuantityFulfilled))
            .ToListAsync(cancellationToken);

        return OrderRefundCalculator.BuildPlan(
            orderId,
            scope,
            combos,
            charges,
            await LoadLiveLineValueAsync(orderId, cancellationToken)
        );
    }

    public async Task<OrderRefundPlan> BuildPlanForAmountAsync(
        int orderId,
        decimal amount,
        CancellationToken cancellationToken
    ) =>
        OrderRefundCalculator.BuildPlanForAmount(
            orderId,
            amount,
            await LoadChargeCapacitiesAsync(orderId, cancellationToken)
        );

    public async Task<IReadOnlyList<OrderRefundReservation>> ReserveAsync(
        OrderRefundPlan plan,
        int? performedByUserId,
        string? note,
        CancellationToken cancellationToken
    )
    {
        var reservations = new List<OrderRefundReservation>();
        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(plan.OrderId, cancellationToken);

                var chargeIds = plan.Items.Select(i => i.ChargeTransactionId).ToList();
                var charges = await db
                    .PaymentTransactions.AsNoTracking()
                    .Where(t => chargeIds.Contains(t.PaymentTransactionId) && t.OrderId == plan.OrderId)
                    .ToDictionaryAsync(t => t.PaymentTransactionId, cancellationToken);

                if (
                    giftCardHandler is null
                    && charges.Values.Any(c => c.Method == PaymentMethod.GiftCard)
                )
                    throw new BusinessRuleException(Messages.Refund_GiftCardHandlerMissing);

                foreach (var item in plan.Items)
                {
                    if (!charges.TryGetValue(item.ChargeTransactionId, out var charge))
                        throw new NotFoundException(Messages.NotFound_Payment);

                    var capacity = OrderRefundCalculator.NetRefundable(
                        await ledger.GetRefundableAmountAsync(
                            charge.PaymentTransactionId,
                            cancellationToken
                        ),
                        charge.GatewayFee
                    );
                    if (item.Amount > capacity)
                        throw new BusinessRuleException(Messages.Refund_ExceedsCollected);

                    var refund =
                        charge.Method == PaymentMethod.GiftCard
                            ? await ReserveGiftCardAsync(
                                charge,
                                item.Amount,
                                note,
                                performedByUserId,
                                cancellationToken
                            )
                            : await refundService.ReserveAsync(
                                charge,
                                item.Amount,
                                note,
                                performedByUserId,
                                cancellationToken
                            );

                    reservations.Add(
                        new OrderRefundReservation(
                            refund.PaymentTransactionId,
                            charge.PaymentTransactionId,
                            charge.Method,
                            refund.Amount,
                            refund.Status
                        )
                    );
                }
            },
            cancellationToken
        );
        return reservations;
    }

    public Task<IReadOnlyList<PaymentRefundResult>> ExecuteAsync(
        IReadOnlyList<OrderRefundReservation> reservations,
        CancellationToken cancellationToken
    ) => ExecuteAsync(reservations, true, cancellationToken);

    public async Task<IReadOnlyList<PaymentRefundResult>> ExecuteAsync(
        IReadOnlyList<OrderRefundReservation> reservations,
        bool failOnError,
        CancellationToken cancellationToken
    )
    {
        var results = new List<PaymentRefundResult>(reservations.Count);
        foreach (var reservation in reservations)
        {
            try
            {
                results.Add(
                    await refundService.ExecuteAsync(
                        reservation.RefundTransactionId,
                        failOnError,
                        cancellationToken
                    )
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "OrderRefundExecuteError RefundTransactionId {RefundId} ChargeTransactionId {ChargeId}",
                    reservation.RefundTransactionId,
                    reservation.ChargeTransactionId
                );
                results.Add(
                    new PaymentRefundResult(
                        reservation.RefundTransactionId,
                        PaymentStatus.Pending,
                        reservation.Amount,
                        Messages.Refund_ExecutionError
                    )
                );
            }
        }

        return results;
    }

    private async Task<decimal?> LoadLiveLineValueAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        if (
            !await db
                .OrderExchanges.AsNoTracking()
                .AnyAsync(e => e.OldOrderLine.OrderId == orderId, cancellationToken)
        )
            return null;

        return await db
                .OrderLines.AsNoTracking()
                .Where(l => l.OrderId == orderId)
                .ExcludingExchangedOut(db)
                .SumAsync(l => (decimal?)(l.PaidAmount - l.RefundedAmount), cancellationToken)
            ?? 0m;
    }

    private async Task<IReadOnlyList<RefundChargeCapacity>> LoadChargeCapacitiesAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        if (!await db.Orders.AsNoTracking().AnyAsync(o => o.OrderId == orderId, cancellationToken))
            throw new NotFoundException(Messages.NotFound_Order);

        var charges = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OrderId == orderId
                && t.Type == PaymentTransactionType.Charge
                && t.Status == PaymentStatus.Success
            )
            .OrderBy(t => t.PaymentTransactionId)
            .Select(t => new
            {
                t.PaymentTransactionId,
                t.Method,
                t.GatewayFee,
            })
            .ToListAsync(cancellationToken);

        var capacities = new List<RefundChargeCapacity>(charges.Count);
        foreach (var charge in charges)
            capacities.Add(
                new RefundChargeCapacity(
                    charge.PaymentTransactionId,
                    charge.Method,
                    await ledger.GetRefundableAmountAsync(
                        charge.PaymentTransactionId,
                        cancellationToken
                    ),
                    charge.GatewayFee
                )
            );
        return capacities;
    }

    private async Task<PaymentTransaction> ReserveGiftCardAsync(
        PaymentTransaction charge,
        decimal amount,
        string? note,
        int? performedByUserId,
        CancellationToken cancellationToken
    )
    {
        var refund = await giftCardHandler!.ReserveAsync(
            charge,
            amount,
            note,
            performedByUserId,
            cancellationToken
        );
        if (refund.Status == PaymentStatus.Success)
            await eventDispatcher.PublishAsync(
                new OrderRefunded(
                    refund.OrderId,
                    refund.PaymentTransactionId,
                    refund.Amount,
                    refund.CompletedAt ?? DateTime.UtcNow
                ),
                cancellationToken
            );
        return refund;
    }
}
