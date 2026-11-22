using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;

namespace Cinema.Application.Utils;

public static class OrderCancellationRules
{
    public static bool IsReasonAllowed(CancelActorKind kind, CancelReason reason) =>
        kind == CancelActorKind.Customer
            ? reason
                is CancelReason.CustomerChangedMind
                    or CancelReason.DuplicateOrder
                    or CancelReason.Other
            : reason
                is CancelReason.CustomerChangedMind
                    or CancelReason.DuplicateOrder
                    or CancelReason.PaymentError
                    or CancelReason.Other;

    public static void EnsureReasonAllowed(CancelActorKind kind, CancelReason reason)
    {
        if (IsReasonAllowed(kind, reason))
            return;

        throw new ValidationAppException(
            Messages.Invalid_CancelReasonNotAllowed,
            new Dictionary<string, string[]> { ["reason"] = [Messages.Invalid_CancelReasonNotAllowed] }
        );
    }

    public static void EnsureRefundEligible(
        decimal refundPercent,
        int deadlineHours,
        DateTime? showtimeStart,
        DateTime now
    )
    {
        if (refundPercent <= 0m)
            throw new BusinessRuleException(Messages.Rule_RefundNotSupported);

        if (showtimeStart is { } start && now > start.AddHours(-deadlineHours))
            throw new BusinessRuleException(
                string.Format(Messages.Rule_RefundDeadlineExceeded, deadlineHours)
            );
    }

    public static bool IsCustomerRefundableMethod(PaymentMethod method) =>
        method.IsGatewayMethod() || method == PaymentMethod.GiftCard;
}
