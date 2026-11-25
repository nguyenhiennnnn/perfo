using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.CounterShifts;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class CounterPaymentService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IGiftCardHoldService giftCardHolds,
    IPaymentRefundService refundService,
    IOrderConfirmationService confirmationService,
    ILogger<CounterPaymentService> logger
) : ICounterPaymentService
{
    public async Task<CounterCheckoutResponse> PayAsync(
        int orderId,
        int performedByUserId,
        CounterCheckoutRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!CounterPaymentMethods.TryParse(request.PaymentMethod, out var method))
            throw new ValidationAppException(
                Messages.Validation_CounterPaymentMethod_Invalid,
                new Dictionary<string, string[]>
                {
                    ["paymentMethod"] = [Messages.Validation_CounterPaymentMethod_Invalid],
                }
            );

        var counterShiftId = await db.RequireOpenCounterShiftIdByUserAsync(
            performedByUserId,
            cancellationToken
        );

        PaymentTransaction charge;
        string orderCode;
        decimal amount;
        decimal? received = null;
        decimal change = 0m;

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.LockOrderAsync(orderId, cancellationToken);

            var order =
                await db
                    .Orders.AsNoTracking()
                    .Where(o => o.OrderId == orderId)
                    .Select(o => new
                    {
                        o.OrderCode,
                        o.Channel,
                        o.Status,
                        o.ExpiresAt,
                    })
                    .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_Order);

            if (order.Channel != SalesChannel.Counter)
                throw new BusinessRuleException(Messages.Rule_CounterPaymentOnlyForCounterOrder);
            if (order.Status != OrderStatus.Pending || order.ExpiresAt <= DateTime.UtcNow)
                throw new BusinessRuleException(Messages.Rule_CounterOrderNotPendingOrExpired);

            var giftCardReserved = await giftCardHolds.GetReservedAmountAsync(
                orderId,
                cancellationToken
            );
            var outstanding =
                (await ledger.GetSummaryAsync(orderId, cancellationToken)).OutstandingAmount
                - giftCardReserved;
            if (outstanding <= 0m)
                throw new BusinessRuleException(
                    giftCardReserved > 0m
                        ? Messages.Rule_GiftCardOrderFullyCovered
                        : Messages.Rule_CounterOrderAlreadyPaid
                );

            amount = request.Amount is { } requested
                ? OrderPricing.RoundToDong(requested)
                : outstanding;
            if (amount <= 0m || amount > outstanding)
                throw new ValidationAppException(
                    Messages.Rule_CounterPaymentAmountInvalid,
                    new Dictionary<string, string[]>
                    {
                        ["amount"] = [Messages.Rule_CounterPaymentAmountInvalid],
                    }
                );

            if (method == PaymentMethod.Cash)
            {
                received = OrderPricing.RoundToDong(request.AmountReceived ?? 0m);
                if (received < amount)
                    throw new BusinessRuleException(Messages.Rule_AmountReceivedInsufficient);
                change = received.Value - amount;
            }

            var now = DateTime.UtcNow;
            charge = new PaymentTransaction
            {
                OrderId = orderId,
                Type = PaymentTransactionType.Charge,
                Method = method,
                Amount = amount,
                TransactionCode = CodeGenerator.GenerateCounterTransactionCode(order.OrderCode),
                Status = PaymentStatus.Success,
                CreatedAt = now,
                CompletedAt = now,
                PerformedByUserId = performedByUserId,
                CounterShiftId = counterShiftId,
                AmountReceived = received,
                ChangeGiven = received is null ? null : change,
            };
            db.PaymentTransactions.Add(charge);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            orderCode = order.OrderCode;
        }

        logger.LogInformation(
            "CounterPaymentRecorded PaymentTransactionId {Id} OrderId {OrderId} Method {Method} Amount {Amount}",
            charge.PaymentTransactionId,
            orderId,
            method,
            amount
        );

        var confirmation = await confirmationService.TryConfirmAsync(orderId, cancellationToken);
        if (confirmation == OrderConfirmationResult.NotPayable)
        {
            await RefundCounterChargesAsync(orderId, performedByUserId, cancellationToken);
            throw new BusinessRuleException(Messages.Rule_CounterOrderNotPayable);
        }

        var summary = await ledger.GetSummaryAsync(orderId, cancellationToken);
        var remaining = Math.Max(
            0m,
            summary.OutstandingAmount
                - await giftCardHolds.GetReservedAmountAsync(orderId, cancellationToken)
        );
        var confirmed =
            confirmation
                is OrderConfirmationResult.Confirmed
                    or OrderConfirmationResult.AlreadyConfirmed;

        return new CounterCheckoutResponse(
            orderId,
            orderCode,
            (confirmed ? OrderStatus.Confirmed : OrderStatus.Pending).ToDtoStatus(),
            charge.PaymentTransactionId,
            charge.TransactionCode,
            method.ToDtoMethod(),
            amount,
            received,
            change,
            summary.NetPaidAmount,
            remaining,
            remaining <= 0m
        );
    }

    private async Task RefundCounterChargesAsync(
        int orderId,
        int performedByUserId,
        CancellationToken cancellationToken
    )
    {
        var charges = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OrderId == orderId
                && t.Type == PaymentTransactionType.Charge
                && t.Status == PaymentStatus.Success
                && (t.Method == PaymentMethod.Cash || t.Method == PaymentMethod.Card)
            )
            .ToListAsync(cancellationToken);

        foreach (var charge in charges)
        {
            var refundable = await ledger.GetRefundableAmountAsync(
                charge.PaymentTransactionId,
                cancellationToken
            );
            if (refundable <= 0m)
                continue;

            await refundService.ReserveAsync(
                charge,
                refundable,
                Messages.Refund_OrderNotPayableNote,
                performedByUserId,
                cancellationToken
            );
        }

        logger.LogWarning(
            "CounterPaymentRefundedNotPayable OrderId {OrderId} ChargeCount {Count}",
            orderId,
            charges.Count
        );
    }
}
