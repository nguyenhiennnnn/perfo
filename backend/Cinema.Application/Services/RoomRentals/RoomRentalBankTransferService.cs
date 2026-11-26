using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalBankTransferService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IGiftCardHoldService giftCardHolds,
    IOrderConfirmationService confirmationService,
    IRoomRentalPaymentOrderService paymentOrders,
    IAuditLogger auditLogger,
    ILogger<RoomRentalBankTransferService> logger
) : IRoomRentalBankTransferService
{
    public async Task<RoomRentalPaymentOrderDto> RecordAsync(
        int requestId,
        RecordRoomRentalBankTransferRequest request,
        int performedByUserId,
        CancellationToken cancellationToken
    )
    {
        var order = await paymentOrders.EnsureOrderAsync(requestId, request.Stage, cancellationToken);
        var orderId = order.OrderId;
        var note = BuildNote(request);

        PaymentTransaction charge = null!;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);

                var current =
                    await db
                        .Orders.AsNoTracking()
                        .Where(o => o.OrderId == orderId)
                        .Select(o => new
                        {
                            o.OrderCode,
                            o.Status,
                            o.ExpiresAt,
                            o.RoomRentalRequestId,
                        })
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_Order);

                if (current.RoomRentalRequestId != requestId)
                    throw new NotFoundException(Messages.NotFound_Order);
                if (current.Status != OrderStatus.Pending)
                    throw new BusinessRuleException(Messages.Rule_RoomRentalOrderNotPending);

                var now = DateTime.UtcNow;
                if (current.ExpiresAt <= now)
                    throw new BusinessRuleException(Messages.Rule_RoomRentalPaymentWindowClosed);

                var reserved = await giftCardHolds.GetReservedAmountAsync(
                    orderId,
                    cancellationToken
                );
                var outstanding =
                    (await ledger.GetSummaryAsync(orderId, cancellationToken)).OutstandingAmount
                    - reserved;
                var amount = request.Amount is { } requested
                    ? OrderPricing.RoundToDong(requested)
                    : outstanding;
                if (amount <= 0m || amount > outstanding)
                    throw new BusinessRuleException(Messages.Rule_RoomRentalBankTransferAmountInvalid);

                charge = new PaymentTransaction
                {
                    OrderId = orderId,
                    Type = PaymentTransactionType.Charge,
                    Method = PaymentMethod.BankTransfer,
                    Amount = amount,
                    TransactionCode = CodeGenerator.GenerateBankTransferTransactionCode(
                        current.OrderCode
                    ),
                    Status = PaymentStatus.Success,
                    CreatedAt = now,
                    CompletedAt = now,
                    PerformedByUserId = performedByUserId,
                    Note = note,
                };
                db.PaymentTransactions.Add(charge);
                await db.SaveChangesAsync(cancellationToken);

                auditLogger.Record(
                    AuditActions.RecordRoomRentalBankTransfer,
                    AuditEntityTypes.Order,
                    orderId.ToString(),
                    null,
                    new
                    {
                        requestId,
                        request.Stage,
                        charge.PaymentTransactionId,
                        charge.TransactionCode,
                        charge.Amount,
                        reference = request.Reference.Trim(),
                    }
                );
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        logger.LogInformation(
            "RoomRentalBankTransferRecorded RequestId {RequestId} OrderId {OrderId} Stage {Stage} Amount {Amount}",
            requestId,
            orderId,
            request.Stage,
            charge.Amount
        );

        var confirmation = await confirmationService.TryConfirmAsync(orderId, cancellationToken);
        if (confirmation == OrderConfirmationResult.NotPayable)
            logger.LogWarning(
                "RoomRentalBankTransferNotConfirmed OrderId {OrderId} PaymentTransactionId {PaymentTransactionId}",
                orderId,
                charge.PaymentTransactionId
            );

        var payments = await paymentOrders.GetAsync(requestId, cancellationToken);
        return payments.Stages.First(s => s.Stage == request.Stage).Order
            ?? throw new NotFoundException(Messages.NotFound_Order);
    }

    private static string BuildNote(RecordRoomRentalBankTransferRequest request)
    {
        var reference = request.Reference.Trim();
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        var combined = note is null ? reference : $"{reference} - {note}";
        return combined.Length <= FieldLengths.ErrorMessage
            ? combined
            : combined[..FieldLengths.ErrorMessage];
    }
}
