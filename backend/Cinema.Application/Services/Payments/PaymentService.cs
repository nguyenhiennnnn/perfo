using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Payments;

public class PaymentService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IPaymentRefundService refundService,
    IOrderConfirmationService confirmationService,
    IEnumerable<IPaymentGateway> paymentGateways,
    ILogger<PaymentService> logger,
    IOptions<StripeOptions>? stripeOptions = null
) : IPaymentService
{
    private readonly string _stripeCurrency = (stripeOptions?.Value ?? new()).Currency;

    public async Task<VnPayIpnResult> ProcessVnPayIpnAsync(
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken
    )
    {
        var gateway = paymentGateways.FindByMethod(PaymentMethodNames.VnPay);
        if (gateway is null)
        {
            logger.LogWarning("PaymentWebhookReceived VNPay nhưng gateway chưa đăng ký trong DI.");
            return new VnPayIpnResult("99", "Gateway not configured");
        }

        var callback = gateway.VerifyCallback(parameters);
        logger.LogInformation(
            "PaymentWebhookReceived Method VNPay TxnRef {TxnRef} ResponseCode {Code}",
            callback.TransactionId,
            callback.GatewayResponseCode
        );

        if (!callback.SignatureValid)
        {
            logger.LogWarning(
                "PaymentWebhookReceived VNPay chữ ký không hợp lệ — TxnRef {TxnRef}",
                callback.TransactionId
            );
            return new VnPayIpnResult("97", "Invalid signature");
        }

        var charge = await FindChargeAsync(
            PaymentMethod.VNPay,
            callback.TransactionId,
            cancellationToken
        );
        if (charge is null)
            return new VnPayIpnResult("01", "Order not found");

        if (IsAlreadyProcessed(charge, callback.IsSuccess))
        {
            LogAlreadyProcessed(charge);
            return new VnPayIpnResult("00", "Confirm Success");
        }

        if (!AmountMatches(charge, callback.Amount))
        {
            logger.LogWarning(
                "PaymentWebhookReceived VNPay sai số tiền — PaymentTransactionId {Id} Expected {Expected} Got {Got}",
                charge.PaymentTransactionId,
                charge.Amount,
                callback.Amount
            );
            return new VnPayIpnResult("04", "Invalid amount");
        }

        await ApplyChargeResultAsync(charge, callback, cancellationToken);
        return new VnPayIpnResult("00", "Confirm Success");
    }

    public async Task<MomoIpnResult> ProcessMomoIpnAsync(
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken
    )
    {
        var gateway = paymentGateways.FindByMethod(PaymentMethodNames.Momo);
        if (gateway is null)
        {
            logger.LogWarning("PaymentWebhookReceived MoMo nhưng gateway chưa đăng ký trong DI.");
            return new MomoIpnResult(99, "Gateway not configured");
        }

        var callback = gateway.VerifyCallback(parameters);
        logger.LogInformation(
            "PaymentWebhookReceived Method Momo OrderId {OrderId} ResultCode {Code}",
            callback.TransactionId,
            callback.GatewayResponseCode
        );

        if (!callback.SignatureValid)
        {
            logger.LogWarning(
                "PaymentWebhookReceived MoMo chữ ký không hợp lệ — OrderId {OrderId}",
                callback.TransactionId
            );
            return new MomoIpnResult(97, "Invalid signature");
        }

        var charge = await FindChargeAsync(
            PaymentMethod.Momo,
            callback.TransactionId,
            cancellationToken
        );
        if (charge is null)
            return new MomoIpnResult(1, "Order not found");

        if (IsAlreadyProcessed(charge, callback.IsSuccess))
        {
            LogAlreadyProcessed(charge);
            return new MomoIpnResult(0, "success");
        }

        if (!AmountMatches(charge, callback.Amount))
            return new MomoIpnResult(4, "Invalid amount");

        await ApplyChargeResultAsync(charge, callback, cancellationToken);
        return new MomoIpnResult(0, "success");
    }

    public async Task<StripeWebhookResult> ProcessStripeWebhookAsync(
        string jsonPayload,
        string? stripeSignature,
        CancellationToken cancellationToken
    )
    {
        var gateway = paymentGateways.FindByMethod(PaymentMethodNames.Stripe);
        if (gateway is null)
        {
            logger.LogWarning("PaymentWebhookReceived Stripe nhưng gateway chưa đăng ký trong DI.");
            return new StripeWebhookResult(false, "Gateway not configured");
        }

        var callback = gateway.VerifyWebhook(jsonPayload, stripeSignature);
        logger.LogInformation(
            "PaymentWebhookReceived Method Stripe TxnRef {TxnRef} Success {IsSuccess}",
            callback.TransactionId,
            callback.IsSuccess
        );

        if (!callback.SignatureValid)
        {
            logger.LogWarning("PaymentWebhookReceived Stripe chữ ký không hợp lệ.");
            return new StripeWebhookResult(false, "Invalid signature");
        }

        if (string.IsNullOrEmpty(callback.TransactionId))
        {
            return new StripeWebhookResult(true, "Event ignored");
        }

        var charge = await FindChargeAsync(
            PaymentMethod.Stripe,
            callback.TransactionId,
            cancellationToken
        );
        if (charge is null)
        {
            logger.LogWarning(
                "PaymentWebhookReceived Stripe Payment not found TxnRef {TxnRef}",
                callback.TransactionId
            );
            return new StripeWebhookResult(false, "Payment not found");
        }

        if (IsAlreadyProcessed(charge, callback.IsSuccess))
        {
            LogAlreadyProcessed(charge);
            return new StripeWebhookResult(true, "Payment already processed");
        }

        if (!AmountMatches(charge, callback.Amount))
        {
            logger.LogWarning(
                "PaymentWebhookReceived Stripe sai số tiền — PaymentTransactionId {Id} Expected {Expected} Got {Got}",
                charge.PaymentTransactionId,
                charge.Amount,
                callback.Amount
            );
            return new StripeWebhookResult(false, "Invalid amount");
        }

        if (
            !string.IsNullOrWhiteSpace(callback.Currency)
            && !string.Equals(
                callback.Currency,
                _stripeCurrency,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            logger.LogWarning(
                "PaymentWebhookReceived Stripe sai currency — PaymentTransactionId {Id} Expected {Expected} Got {Got}",
                charge.PaymentTransactionId,
                _stripeCurrency,
                callback.Currency
            );
            return new StripeWebhookResult(false, "Invalid currency");
        }

        await ApplyChargeResultAsync(charge, callback, cancellationToken);
        return new StripeWebhookResult(true, "Payment processed successfully");
    }

    private Task<PaymentTransaction?> FindChargeAsync(
        PaymentMethod method,
        string transactionCode,
        CancellationToken cancellationToken
    ) =>
        db
            .PaymentTransactions.AsNoTracking()
            .FirstOrDefaultAsync(
                t =>
                    t.TransactionCode == transactionCode
                    && t.Type == PaymentTransactionType.Charge
                    && t.Method == method,
                cancellationToken
            );

    private async Task ApplyChargeResultAsync(
        PaymentTransaction pending,
        GatewayCallbackResult callback,
        CancellationToken cancellationToken
    )
    {
        var isSuccess = callback.IsSuccess;
        var status = isSuccess ? PaymentStatus.Success : PaymentStatus.Failed;
        var completedAt = (DateTime?)DateTime.UtcNow;
        var responseCode = callback.GatewayResponseCode;
        var gatewayTransactionId = callback.GatewayTransactionId;
        var chargeId = pending.PaymentTransactionId;

        PaymentTransaction? charge = null;
        var refunds = new List<PaymentTransaction>();

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.LockOrderAsync(pending.OrderId, cancellationToken);

            var claimable = db.PaymentTransactions.Where(t =>
                t.PaymentTransactionId == chargeId
                && (t.Status == PaymentStatus.Pending || (isSuccess && t.Status == PaymentStatus.Failed))
            );

            var claimed =
                isSuccess && !string.IsNullOrWhiteSpace(gatewayTransactionId)
                    ? await claimable.ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(t => t.Status, status)
                                .SetProperty(t => t.GatewayResponseCode, responseCode)
                                .SetProperty(t => t.CompletedAt, completedAt)
                                .SetProperty(t => t.GatewayTransactionId, gatewayTransactionId),
                        cancellationToken
                    )
                    : await claimable.ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(t => t.Status, status)
                                .SetProperty(t => t.GatewayResponseCode, responseCode)
                                .SetProperty(t => t.CompletedAt, completedAt),
                        cancellationToken
                    );

            if (claimed == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                logger.LogInformation(
                    "PaymentDuplicateIgnored PaymentTransactionId {Id}",
                    chargeId
                );
                return;
            }

            if (!isSuccess)
            {
                await transaction.CommitAsync(cancellationToken);
                logger.LogInformation(
                    "PaymentFailed PaymentTransactionId {Id} OrderId {OrderId}",
                    chargeId,
                    pending.OrderId
                );
                return;
            }

            charge = await db
                .PaymentTransactions.AsNoTracking()
                .FirstAsync(t => t.PaymentTransactionId == chargeId, cancellationToken);

            var summary = await ledger.GetSummaryAsync(pending.OrderId, cancellationToken);
            if (summary.OverpaidAmount > 0m)
            {
                refunds.Add(
                    await refundService.ReserveAsync(
                        charge,
                        Math.Min(summary.OverpaidAmount, charge.Amount),
                        Messages.Refund_OverpaymentNote,
                        null,
                        cancellationToken
                    )
                );
            }

            await transaction.CommitAsync(cancellationToken);
        }

        logger.LogInformation(
            "PaymentRecorded PaymentTransactionId {Id} OrderId {OrderId} Amount {Amount}",
            chargeId,
            pending.OrderId,
            pending.Amount
        );

        var confirmation = await confirmationService.TryConfirmAsync(
            pending.OrderId,
            cancellationToken
        );
        if (confirmation == OrderConfirmationResult.NotPayable)
        {
            var notPayableRefund = await ReserveRemainingRefundAsync(
                charge!,
                Messages.Refund_OrderNotPayableNote,
                cancellationToken
            );
            if (notPayableRefund is not null)
                refunds.Add(notPayableRefund);
        }

        foreach (var refund in refunds)
            await ExecuteRefundAsync(refund, chargeId, pending.OrderId, cancellationToken);
    }

    private async Task<PaymentTransaction?> ReserveRemainingRefundAsync(
        PaymentTransaction charge,
        string note,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockOrderAsync(charge.OrderId, cancellationToken);

        var refundable = await ledger.GetRefundableAmountAsync(
            charge.PaymentTransactionId,
            cancellationToken
        );
        PaymentTransaction? refund = null;
        if (refundable > 0m)
            refund = await refundService.ReserveAsync(charge, refundable, note, null, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return refund;
    }

    private async Task ExecuteRefundAsync(
        PaymentTransaction refund,
        int chargeId,
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var result = await refundService.ExecuteAsync(
            refund.PaymentTransactionId,
            cancellationToken
        );
        if (result.Success)
        {
            logger.LogInformation(
                "ChargeRefunded PaymentTransactionId {Id} RefundTransactionId {RefundId} Amount {Amount}",
                chargeId,
                result.RefundTransactionId,
                result.Amount
            );
            return;
        }

        await db
            .PaymentTransactions.Where(t => t.PaymentTransactionId == chargeId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.NeedsManualRefund, true),
                cancellationToken
            );
        logger.LogCritical(
            "ChargeRefundFailed PaymentTransactionId {Id} OrderId {OrderId} Amount {Amount} — cần hoàn tiền thủ công.",
            chargeId,
            orderId,
            result.Amount
        );
    }

    private static bool IsAlreadyProcessed(PaymentTransaction charge, bool callbackSuccess) =>
        charge.Status == PaymentStatus.Success
        || (charge.Status == PaymentStatus.Failed && !callbackSuccess);

    private static bool AmountMatches(PaymentTransaction charge, decimal callbackAmount) =>
        Math.Abs(charge.Amount - callbackAmount) <= 0.01m;

    private void LogAlreadyProcessed(PaymentTransaction charge) =>
        logger.LogInformation(
            "PaymentAlreadyProcessed PaymentTransactionId {Id} Status {Status}",
            charge.PaymentTransactionId,
            charge.Status
        );
}
