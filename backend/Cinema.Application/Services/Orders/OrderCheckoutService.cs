using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderCheckoutService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IGiftCardHoldService giftCardHolds,
    IEnumerable<IPaymentGateway> paymentGateways,
    ILogger<OrderCheckoutService> logger
) : IOrderCheckoutService
{
    public async Task<CheckoutResponse> CheckoutAsync(
        int userId,
        int orderId,
        CheckoutRequest request,
        CancellationToken cancellationToken
    )
    {
        var order =
            await db
                .Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);
        if (order.UserId != userId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);
        if (order.Status != OrderStatus.Pending || order.ExpiresAt <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_BookingNotPendingOrExpired);

        var gateway =
            paymentGateways.FindByMethod(request.PaymentMethod)
            ?? throw new BusinessRuleException(
                string.Format(Messages.Rule_PaymentGatewayNotConfigured, request.PaymentMethod)
            );

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
                    : Messages.Rule_BookingNotPendingOrExpired
            );

        await db
            .PaymentTransactions.Where(t =>
                t.OrderId == orderId
                && t.Type == PaymentTransactionType.Charge
                && t.Status == PaymentStatus.Pending
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.Status, PaymentStatus.Failed),
                cancellationToken
            );

        var transactionCode = CodeGenerator.GeneratePaymentTransactionId(order.OrderCode);
        var nowUtc = DateTime.UtcNow;
        var createdAt = new DateTime(
            nowUtc.Ticks - nowUtc.Ticks % TimeSpan.TicksPerSecond,
            DateTimeKind.Utc
        );

        var gatewayResult = await gateway.CreatePaymentAsync(
            new CreatePaymentRequest(
                order.OrderId,
                outstanding,
                string.Format(Messages.Payment_BookingDescription, order.OrderCode),
                transactionCode,
                createdAt
            ),
            cancellationToken
        );

        var charge = new PaymentTransaction
        {
            OrderId = order.OrderId,
            Type = PaymentTransactionType.Charge,
            Method = EnumConverters.ParseEnum<PaymentMethod>(
                request.PaymentMethod,
                "paymentMethod"
            ),
            Amount = outstanding,
            TransactionCode = transactionCode,
            Status = PaymentStatus.Pending,
            CreatedAt = createdAt,
            PerformedByUserId = userId,
        };
        db.PaymentTransactions.Add(charge);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "PaymentCreated PaymentTransactionId {PaymentTransactionId} OrderId {OrderId} Method {Method}",
            charge.PaymentTransactionId,
            order.OrderId,
            request.PaymentMethod
        );

        return new CheckoutResponse(gatewayResult.PaymentUrl, charge.PaymentTransactionId);
    }
}
